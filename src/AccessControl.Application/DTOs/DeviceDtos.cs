using AccessControl.Domain.Enums;

namespace AccessControl.Application.DTOs;

public record DeviceDto(
    Guid Id, string Name, string? Manufacturer, string? Model, string? IpAddress,
    DeviceStatus Status, DriverType DriverType, bool TieneCredenciales, List<DeviceAccessPointDto> AccessPoints, Guid TenantId, string TenantName);

public record DeviceAccessPointDto(Guid AccessPointId, string AccessPointName, int ChannelNumber);

// Username/Password/Port/UseHttps solo aplican para drivers que hablan HTTP a un panel real
// (por ahora Hikvision). Para Mock se ignoran. La contraseña nunca se vuelve a devolver en
// las respuestas de la API una vez guardada (ver DeviceDto.TieneCredenciales).
public record DeviceCreateDto(
    string Name, string? Manufacturer, string? Model, string? IpAddress, DriverType DriverType,
    string? Username, string? Password, int? Port, bool? UseHttps, Guid? TenantId = null);

public record DeviceLinkAccessPointDto(Guid AccessPointId, int ChannelNumber);

public record DeviceTestConnectionResultDto(bool Reachable, string? Details);
