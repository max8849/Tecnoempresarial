using AccessControl.Domain.Common;
using AccessControl.Domain.Enums;

namespace AccessControl.Domain.Entities;

public class House : AuditableEntity, ITenantOwned
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Number { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public HouseStatus Status { get; set; } = HouseStatus.Active;

    public ICollection<Resident> Residents { get; set; } = new List<Resident>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<AccessPass> AccessPasses { get; set; } = new List<AccessPass>();
}
