using AccessControl.Domain.Common;
using AccessControl.Domain.Enums;

namespace AccessControl.Domain.Entities;

// Representa una puerta/pluma/portón físico. Independiente de qué dispositivo(s) la controlan.
public class AccessPoint : AuditableEntity, ITenantOwned
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public AccessPointType Type { get; set; }
    public AccessDirection Direction { get; set; } = AccessDirection.Both;
    public bool IsActive { get; set; } = true;

    public ICollection<DeviceAccessPoint> DeviceLinks { get; set; } = new List<DeviceAccessPoint>();
}
