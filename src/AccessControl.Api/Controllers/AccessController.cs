using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Application.Services;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/access")]
[Authorize(Roles = AppRoles.Guardia + "," + AppRoles.AdminPrivada + "," + AppRoles.SuperAdmin)]
[EnableRateLimiting("access-scan")]
public class AccessController : ControllerBase
{
    private readonly IAccessValidationService _service;
    private readonly ITenantContext _tenant;

    public AccessController(IAccessValidationService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    // Solo lectura/preview: para mostrar semáforo en la app del guardia. No abre nada ni consume usos.
    [HttpPost("validate")]
    public async Task<ActionResult<ValidateTokenResponseDto>> Validate(ValidateTokenRequestDto dto) =>
        Ok(await _service.ValidatePreviewAsync(dto.Token));

    // Único endpoint autoritativo: valida desde cero, consume el uso de forma atómica y
    // dispara la apertura física a través de IAccessController. Esto es lo que llama la app del guardia.
    [HttpPost("scan")]
    public async Task<ActionResult<ScanResponseDto>> Scan(ScanRequestDto dto) =>
        Ok(await _service.ScanAsync(dto, _tenant.UserId, HttpContext.Connection.RemoteIpAddress?.ToString()));
}
