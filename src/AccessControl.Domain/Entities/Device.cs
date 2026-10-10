using AccessControl.Domain.Common;
using AccessControl.Domain.Enums;

namespace AccessControl.Domain.Entities;

// Un controlador/panel físico (Shelly, panel Hikvision, relay genérico...).
// Un mismo Device puede controlar varias AccessPoint distintas (paneles multi-canal),
// por eso la relación con AccessPoint es N:M a través de DeviceAccessPoint, no 1:N.
public class Device : AuditableEntity, ITenantOwned
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? IpAddress { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Unknown;
    public DriverType DriverType { get; set; } = DriverType.Mock;

    // Configuración específica del fabricante/driver (credenciales referenciadas, canal, etc.)
    // Se guarda como JSON flexible para no migrar el esquema cuando cambie el hardware real.
    public string? ConfigurationJson { get; set; }

    public ICollection<DeviceAccessPoint> AccessPointLinks { get; set; } = new List<DeviceAccessPoint>();
}
