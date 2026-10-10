using AccessControl.Domain.Agents;

namespace AccessControl.Application.Common.Interfaces;

// Abstrae "avisarle al agente local de una privada" sin que Application ni Infrastructure
// sepan nada de SignalR — eso vive en la Api (composition root), que es quien implementa esto.
public interface IAgentNotifier
{
    bool EstaConectado(Guid tenantId);
    Task<bool> EnviarComandoAperturaAsync(Guid tenantId, AbrirPuertaComando comando, CancellationToken ct = default);
}
