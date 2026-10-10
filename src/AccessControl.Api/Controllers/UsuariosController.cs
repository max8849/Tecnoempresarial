using AccessControl.Application.Common;
using AccessControl.Application.Common.Exceptions;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.AdminPrivada)]
public class UsuariosController : ControllerBase
{
    // Un administrador de privada sólo administra cuentas de residente y guardia de su propia privada.
    private static readonly string[] RolesAdministrablesPorPrivada = { AppRoles.Residente, AppRoles.Guardia };
    private static readonly string[] RolesValidos = { AppRoles.SuperAdmin, AppRoles.AdminPrivada, AppRoles.Residente, AppRoles.Guardia };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public UsuariosController(UserManager<ApplicationUser> userManager, IAppDbContext db, ITenantContext tenant)
    {
        _userManager = userManager;
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> Listar()
    {
        var consulta = _userManager.Users.AsQueryable();
        if (!_tenant.IsSuperAdmin) consulta = consulta.Where(u => u.TenantId == _tenant.TenantId);

        var usuarios = await consulta.OrderBy(u => u.FullName).ToListAsync();
        var residentes = await _db.Residents.Include(r => r.House)
            .Where(r => r.UserId != null)
            .ToDictionaryAsync(r => r.UserId!.Value);
        var nombres = await TenantScope.NombresAsync(_db, usuarios.Where(u => u.TenantId != null).Select(u => u.TenantId!.Value));

        var resultado = new List<UsuarioDto>();
        foreach (var u in usuarios)
        {
            var rol = (await _userManager.GetRolesAsync(u)).FirstOrDefault() ?? string.Empty;
            if (!_tenant.IsSuperAdmin && !RolesAdministrablesPorPrivada.Contains(rol)) continue;

            residentes.TryGetValue(u.Id, out var residente);
            resultado.Add(new UsuarioDto(
                u.Id, u.Email!, u.FullName, rol, u.TenantId,
                u.TenantId is { } t ? nombres.GetValueOrDefault(t, "") : "Todas (superadmin)",
                residente?.House?.Number, residente?.Id,
                !await _userManager.IsLockedOutAsync(u)));
        }
        return Ok(resultado);
    }

    [HttpGet("residentes-disponibles")]
    public async Task<ActionResult<List<ResidenteSinCuentaDto>>> ResidentesDisponibles()
    {
        var residentes = await _db.Residents.Include(r => r.House)
            .Where(r => r.UserId == null)
            .OrderBy(r => r.LastName)
            .ToListAsync();
        var nombres = await TenantScope.NombresAsync(_db, residentes.Select(r => r.TenantId));

        return Ok(residentes.Select(r => new ResidenteSinCuentaDto(
            r.Id, $"{r.FirstName} {r.LastName}", r.House?.Number ?? "", r.TenantId, nombres.GetValueOrDefault(r.TenantId, ""))).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Crear(CrearUsuarioDto dto)
    {
        if (!RolesValidos.Contains(dto.Role))
            throw new ValidationAppException("Rol no válido.");

        if (!_tenant.IsSuperAdmin && !RolesAdministrablesPorPrivada.Contains(dto.Role))
            throw new ForbiddenException("Solo puedes crear cuentas de residente o guardia.");

        var email = dto.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) is not null)
            throw new ValidationAppException("Ya existe una cuenta con ese correo.");

        Resident? residente = null;
        Guid? tenantId = _tenant.IsSuperAdmin ? dto.TenantId : _tenant.TenantId;

        if (dto.Role == AppRoles.Residente)
        {
            if (dto.ResidentId is null)
                throw new ValidationAppException("Selecciona el residente al que pertenece la cuenta.");
            residente = await _db.Residents.FirstOrDefaultAsync(r => r.Id == dto.ResidentId.Value)
                ?? throw new NotFoundException(nameof(Resident), dto.ResidentId.Value);
            if (residente.UserId is not null)
                throw new ValidationAppException("Ese residente ya tiene una cuenta.");
            tenantId = residente.TenantId;
        }

        if (dto.Role == AppRoles.SuperAdmin)
        {
            tenantId = null;
        }
        else
        {
            if (tenantId is null) throw new ValidationAppException("Indica la privada de la cuenta.");
            if (!await _db.Tenants.AnyAsync(t => t.Id == tenantId.Value))
                throw new NotFoundException("Tenant", tenantId.Value);
        }

        var usuario = new ApplicationUser
        {
            UserName = email, Email = email, FullName = dto.FullName.Trim(), TenantId = tenantId, EmailConfirmed = true
        };
        var creado = await _userManager.CreateAsync(usuario, dto.Password);
        if (!creado.Succeeded)
            throw new ValidationAppException(MensajesContrasena.Traducir(creado.Errors));

        await _userManager.AddToRoleAsync(usuario, dto.Role);

        if (residente is not null)
        {
            residente.UserId = usuario.Id;
            await _db.SaveChangesAsync();
        }

        return Ok(new { usuario.Id, usuario.Email });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, ActualizarUsuarioDto dto)
    {
        var usuario = await ObtenerEnAlcanceAsync(id);
        usuario.FullName = dto.FullName.Trim();
        await _userManager.UpdateAsync(usuario);
        return NoContent();
    }

    [HttpPost("{id:guid}/desactivar")]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        var usuario = await ObtenerEnAlcanceAsync(id);
        if (usuario.Id == _tenant.UserId)
            throw new ValidationAppException("No puedes desactivar tu propia cuenta.");

        await _userManager.SetLockoutEnabledAsync(usuario, true);
        await _userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.UtcNow.AddYears(100));
        await RevocarSesionesAsync(usuario.Id);
        return NoContent();
    }

    [HttpPost("{id:guid}/activar")]
    public async Task<IActionResult> Activar(Guid id)
    {
        var usuario = await ObtenerEnAlcanceAsync(id);
        await _userManager.SetLockoutEndDateAsync(usuario, null);
        return NoContent();
    }

    [HttpPost("{id:guid}/restablecer-contrasena")]
    public async Task<IActionResult> RestablecerContrasena(Guid id, RestablecerContrasenaDto dto)
    {
        var usuario = await ObtenerEnAlcanceAsync(id);
        var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        var resultado = await _userManager.ResetPasswordAsync(usuario, token, dto.NuevaContrasena);
        if (!resultado.Succeeded)
            throw new ValidationAppException(MensajesContrasena.Traducir(resultado.Errors));

        await RevocarSesionesAsync(usuario.Id);
        return NoContent();
    }

    private async Task<ApplicationUser> ObtenerEnAlcanceAsync(Guid id)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException("Usuario", id);

        if (_tenant.IsSuperAdmin) return usuario;

        var roles = await _userManager.GetRolesAsync(usuario);
        if (usuario.TenantId != _tenant.TenantId || !roles.Any(r => RolesAdministrablesPorPrivada.Contains(r)))
            throw new ForbiddenException("No puedes administrar esta cuenta.");

        return usuario;
    }

    private async Task RevocarSesionesAsync(Guid userId)
    {
        var sesiones = await _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync();
        foreach (var s in sesiones) s.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
