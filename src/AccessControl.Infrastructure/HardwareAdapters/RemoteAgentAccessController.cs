using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Agents;
using AccessControl.Domain.Entities;

namespace AccessControl.Infrastructure.HardwareAdapters;

// Se usa cuando el backend está centralizado (ej. en la nube) y NO tiene ruta de red
// directa hacia el panel Hikvision de la privada. En vez de llamar ISAPI él mismo,
// le manda el comando al Agente local de esa privada a través de la conexión de
// SignalR que el propio Agente dejó abierta — el Agente es quien realmente llama
// al Hikvision, usando la misma configuración (IP, usuario, contraseña) guardada
// en Device.ConfigurationJson.
public class RemoteAgentAccessController : IAccessController
{
    private readonly IAgentNotifier _agentNotifier;

    public RemoteAgentAccessController(IAgentNotifier agentNotifier)
    {
        _agentNotifier = agentNotifier;
    }

    public Task<ConnectionStatusResult> ValidateConnectionAsync(Device device, CancellationToken ct = default)
    {
        var conectado = _agentNotifier.EstaConectado(device.TenantId);
        return Task.FromResult(new ConnectionStatusResult(
            conectado, conectado ? "El agente local de esta privada está conectado." : "El agente local de esta privada no está conectado ahora mismo."));
    }

    public async Task<OpenResult> OpenAsync(Device device, OpenRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(device.IpAddress))
            return new OpenResult(false, null, $"El dispositivo '{device.Name}' no tiene una IP configurada.");

        HikvisionConfig config;
        try
        {
            config = HikvisionConfig.Leer(device);
        }
        catch (InvalidOperationException ex)
        {
            return new OpenResult(false, null, ex.Message);
        }

        var comando = new AbrirPuertaComando(
            device.Id, device.IpAddress, config.Port, config.UseHttps, config.Username, config.Password, request.ChannelNumber);

        var enviado = await _agentNotifier.EnviarComandoAperturaAsync(device.TenantId, comando, ct);

        return enviado
            ? new OpenResult(true, "Comando enviado al agente local vía SignalR.", null)
            : new OpenResult(false, null, "El agente local de esta privada no está conectado en este momento.");
    }

    public async Task<DeviceStatusResult> GetStatusAsync(Device device, CancellationToken ct = default)
    {
        var estado = await ValidateConnectionAsync(device, ct);
        return new DeviceStatusResult(estado.IsReachable, estado.Details);
    }
}
