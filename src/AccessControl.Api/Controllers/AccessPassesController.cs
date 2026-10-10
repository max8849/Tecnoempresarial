using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Application.Services;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/access-passes")]
[Authorize]
public class AccessPassesController : ControllerBase
{
    private readonly IAccessPassService _service;
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AccessPassesController(IAccessPassService service, IAppDbContext db, ITenantContext tenant)
    {
        _service = service;
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccessPassDto>>> GetAll([FromQuery] Guid? houseId) => Ok(await _service.GetAllAsync(houseId));

    [HttpPost]
    public async Task<ActionResult<AccessPassCreatedDto>> Create(AccessPassCreateDto dto)
    {
        // Un RESIDENTE solo puede generar QRs para su propia casa, nunca para cualquier otra.
        if (User.IsInRole(AppRoles.Residente) && !User.IsInRole(AppRoles.AdminPrivada))
        {
            var esSuCasa = await _db.Residents.AnyAsync(r => r.UserId == _tenant.UserId && r.HouseId == dto.HouseId);
            if (!esSuCasa) return Forbid();
        }

        var creado = await _service.CreateAsync(dto, _tenant.UserId!.Value);
        return Ok(creado);
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        await _service.RevokeAsync(id, _tenant.UserId!.Value);
        return NoContent();
    }
}
