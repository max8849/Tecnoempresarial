using AccessControl.Application.DTOs;
using AccessControl.Application.Services;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _service;

    public TenantsController(ITenantService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<TenantDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPost]
    public async Task<ActionResult<TenantDto>> Create(TenantCreateDto dto) => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, TenantUpdateDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    // Clave que hay que copiar a la configuración del programa Agente instalado en la
    // privada, más si su conexión de SignalR está activa ahora mismo.
    [HttpGet("{id:guid}/agent-key")]
    public async Task<ActionResult<TenantAgentKeyDto>> GetAgentKey(Guid id) => Ok(await _service.GetAgentKeyAsync(id));

    [HttpPost("{id:guid}/agent-key/regenerate")]
    public async Task<ActionResult<TenantAgentKeyDto>> RegenerateAgentKey(Guid id) => Ok(await _service.RegenerateAgentKeyAsync(id));
}
