using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService, IAppDbContext db, IConfiguration config)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto)
    {
        var usuario = await _userManager.FindByEmailAsync(dto.Email);
        if (usuario is null || !await _userManager.CheckPasswordAsync(usuario, dto.Password))
            return Unauthorized(new { mensaje = "Credenciales inválidas." });

        if (await _userManager.IsLockedOutAsync(usuario))
            return Unauthorized(new { mensaje = "Esta cuenta está desactivada. Contacta al administrador." });

        var roles = await _userManager.GetRolesAsync(usuario);
        return Ok(await EmitirTokensAsync(usuario, roles));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponseDto>> Refresh(RefreshTokenRequestDto dto)
    {
        var hash = _jwtTokenService.HashRefreshToken(dto.RefreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (stored is null)
            return Unauthorized(new { mensaje = "Refresh token inválido." });

        if (!stored.IsActive)
        {
            // Reuso de un token ya revocado/expirado: señal de posible robo.
            // Se revoca toda la sesión del usuario como medida de contención.
            var otros = await _db.RefreshTokens.Where(t => t.UserId == stored.UserId && t.RevokedAt == null).ToListAsync();
            foreach (var t in otros) t.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Unauthorized(new { mensaje = "Refresh token inválido. La sesión fue revocada por seguridad." });
        }

        var usuario = await _userManager.FindByIdAsync(stored.UserId.ToString());
        if (usuario is null || await _userManager.IsLockedOutAsync(usuario)) return Unauthorized();

        stored.RevokedAt = DateTime.UtcNow;
        var roles = await _userManager.GetRolesAsync(usuario);
        var respuesta = await EmitirTokensAsync(usuario, roles);

        var nuevoHash = _jwtTokenService.HashRefreshToken(respuesta.RefreshToken);
        var nuevo = await _db.RefreshTokens.FirstAsync(t => t.TokenHash == nuevoHash);
        stored.ReplacedByTokenId = nuevo.Id;
        await _db.SaveChangesAsync();

        return Ok(respuesta);
    }

    [HttpPost("cambiar-contrasena")]
    [Authorize]
    public async Task<IActionResult> CambiarContrasena(CambiarContrasenaDto dto)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();

        var usuario = await _userManager.FindByIdAsync(userId.ToString());
        if (usuario is null) return Unauthorized();

        var resultado = await _userManager.ChangePasswordAsync(usuario, dto.ContrasenaActual, dto.ContrasenaNueva);
        if (!resultado.Succeeded)
            return BadRequest(new { mensaje = MensajesContrasena.Traducir(resultado.Errors) });

        // Se cierran las demás sesiones de esta cuenta: quien conozca la contraseña anterior ya no sirve.
        var sesiones = await _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync();
        foreach (var s in sesiones) s.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<LoginResponseDto> EmitirTokensAsync(ApplicationUser usuario, IList<string> roles)
    {
        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(
            usuario.Id, usuario.Email!, usuario.FullName, roles, usuario.TenantId);

        var (rawRefresh, refreshHash) = _jwtTokenService.GenerateRefreshToken();
        var refreshDias = double.Parse(_config["Jwt:RefreshTokenDays"] ?? "30");

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = usuario.Id,
            TokenHash = refreshHash,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDias),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync();

        return new LoginResponseDto(accessToken, expiresAt, rawRefresh, usuario.Email!, usuario.FullName,
            roles.FirstOrDefault() ?? string.Empty, usuario.TenantId);
    }
}
