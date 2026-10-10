using AccessControl.Domain.Common;

namespace AccessControl.Domain.Entities;

public class Vehicle : AuditableEntity, ITenantOwned
{
    public Guid TenantId { get; set; }

    public Guid HouseId { get; set; }
    public House? House { get; set; }

    public string Plate { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
}
