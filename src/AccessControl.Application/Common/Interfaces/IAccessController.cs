using AccessControl.Domain.Entities;

namespace AccessControl.Application.Common.Interfaces;

// Abstracción de "hablar con el hardware físico". El resto de la aplicación nunca sabe
// si detrás hay un Shelly, un panel Hikvision, un relay genérico o el Mock de desarrollo.
public interface IAccessController
{
    Task<ConnectionStatusResult> ValidateConnectionAsync(Device device, CancellationToken ct = default);
    Task<OpenResult> OpenAsync(Device device, OpenRequest request, CancellationToken ct = default);
    Task<DeviceStatusResult> GetStatusAsync(Device device, CancellationToken ct = default);
}

// Resuelve qué IAccessController usar según el DriverType configurado en el Device,
// para que el llamador nunca tenga que hacer ese switch.
public interface IAccessControllerFactory
{
    IAccessController Resolve(Device device);
}

public record OpenRequest(int ChannelNumber, TimeSpan? PulseDuration, Guid? InitiatedByUserId, Guid? AccessEventId);

public record OpenResult(bool CommandAccepted, string? RawProviderResponse, string? ErrorMessage);

public record ConnectionStatusResult(bool IsReachable, string? Details);

public record DeviceStatusResult(bool IsOnline, string? RawStatus);
