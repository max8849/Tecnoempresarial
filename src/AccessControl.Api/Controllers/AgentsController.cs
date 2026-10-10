using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

// El programa Agente llama a /token con su AgentApiKey para obtener un JWT de corta vida
// con el rol técnico AGENTE_LOCAL, y con ese JWT se conecta al Hub de SignalR.
[ApiController]
[Route("api/agents")]
public class AgentsController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly IJwtTokenService _jwt;

    public AgentsController(IAppDbContext db, IJwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<ActionResult<AgentTokenResponseDto>> ObtenerToken(AgentTokenRequestDto dto)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.AgentApiKey == dto.AgentApiKey);
        if (tenant is null || !tenant.IsActive)
            return Unauthorized(new { mensaje = "Clave de agente inválida." });

        var (token, expiresAt) = _jwt.GenerateAccessToken(
            Guid.Empty, $"agente@{tenant.Slug}", $"Agente {tenant.Name}", new[] { AppRoles.AgenteLocal }, tenant.Id);

        return Ok(new AgentTokenResponseDto(token, expiresAt, tenant.Name));
    }
}
