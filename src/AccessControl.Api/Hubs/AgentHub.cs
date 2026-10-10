using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AccessControl.Api.Hubs;

// El Agente local de cada privada se conecta aquí y se queda conectado indefinidamente
// (ver AccessControl.Agent). Solo entidades con el rol técnico AGENTE_LOCAL pueden
// conectarse — nunca un usuario humano.
[Authorize(Roles = AppRoles.AgenteLocal)]
public class AgentHub : Hub
{
    private readonly IAgentPresenceTracker _presencia;

    public AgentHub(IAgentPresenceTracker presencia)
    {
        _presencia = presencia;
    }

    public override async Task OnConnectedAsync()
    {
        var tenantId = ObtenerTenantId();
        if (tenantId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoDe(tenantId.Value));
            _presencia.Conectado(tenantId.Value);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = ObtenerTenantId();
        if (tenantId.HasValue) _presencia.Desconectado(tenantId.Value);
        await base.OnDisconnectedAsync(exception);
    }

    private Guid? ObtenerTenantId() =>
        Guid.TryParse(Context.User?.FindFirst("tenantId")?.Value, out var id) ? id : null;

    public static string GrupoDe(Guid tenantId) => $"agente-tenant-{tenantId}";
}
