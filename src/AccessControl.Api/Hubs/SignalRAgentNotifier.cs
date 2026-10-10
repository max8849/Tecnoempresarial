using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Agents;
using Microsoft.AspNetCore.SignalR;

namespace AccessControl.Api.Hubs;

public class SignalRAgentNotifier : IAgentNotifier
{
    private readonly IHubContext<AgentHub> _hub;
    private readonly IAgentPresenceTracker _presencia;

    public SignalRAgentNotifier(IHubContext<AgentHub> hub, IAgentPresenceTracker presencia)
    {
        _hub = hub;
        _presencia = presencia;
    }

    public bool EstaConectado(Guid tenantId) => _presencia.EstaConectado(tenantId);

    public async Task<bool> EnviarComandoAperturaAsync(Guid tenantId, AbrirPuertaComando comando, CancellationToken ct = default)
    {
        if (!EstaConectado(tenantId)) return false;

        await _hub.Clients.Group(AgentHub.GrupoDe(tenantId)).SendAsync("AbrirCanal", comando, ct);
        return true;
    }
}
