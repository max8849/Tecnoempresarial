using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.HardwareAdapters;

// Implementación de desarrollo/demo: nunca habla con hardware real, siempre "abre" con éxito
// tras una pequeña simulación de latencia. Permite probar todo el flujo de negocio sin
// depender de que ya sepamos el modelo exacto de Hikvision ni tengamos un Shelly a la mano.
public class MockAccessController : IAccessController
{
    private readonly ILogger<MockAccessController> _logger;

    public MockAccessController(ILogger<MockAccessController> logger)
    {
        _logger = logger;
    }

    public async Task<ConnectionStatusResult> ValidateConnectionAsync(Device device, CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        return new ConnectionStatusResult(true, "Mock: conexión simulada correctamente.");
    }

    public async Task<OpenResult> OpenAsync(Device device, OpenRequest request, CancellationToken ct = default)
    {
        await Task.Delay(150, ct);
        _logger.LogInformation(
            "[MOCK] Pulso simulado en dispositivo {DeviceName} ({DeviceId}), canal {Channel}",
            device.Name, device.Id, request.ChannelNumber);
        return new OpenResult(true, $"MOCK_OK canal={request.ChannelNumber}", null);
    }

    public async Task<DeviceStatusResult> GetStatusAsync(Device device, CancellationToken ct = default)
    {
        await Task.Delay(30, ct);
        return new DeviceStatusResult(true, "MOCK_ONLINE");
    }
}
