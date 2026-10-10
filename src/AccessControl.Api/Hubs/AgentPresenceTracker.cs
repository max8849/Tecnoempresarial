using System.Collections.Concurrent;

namespace AccessControl.Api.Hubs;

public class AgentPresenceTracker : IAgentPresenceTracker
{
    // Cuenta conexiones activas por tenant (un agente reconectándose puede pasar por 0
    // brevemente; contar en vez de usar un simple bool evita falsos "desconectado").
    private readonly ConcurrentDictionary<Guid, int> _conexionesPorTenant = new();

    public void Conectado(Guid tenantId) => _conexionesPorTenant.AddOrUpdate(tenantId, 1, (_, actual) => actual + 1);

    public void Desconectado(Guid tenantId) => _conexionesPorTenant.AddOrUpdate(tenantId, 0, (_, actual) => Math.Max(0, actual - 1));

    public bool EstaConectado(Guid tenantId) => _conexionesPorTenant.TryGetValue(tenantId, out var n) && n > 0;
}
