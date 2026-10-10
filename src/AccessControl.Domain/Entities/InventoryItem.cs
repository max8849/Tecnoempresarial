using AccessControl.Domain.Common;
using AccessControl.Domain.Enums;

namespace AccessControl.Domain.Entities;

// Inventario físico de equipo de cada privada: plumas, motores, paneles Hikvision, cámaras,
// relays, cableado, etc. Es independiente de Device: un InventoryItem es un activo físico con
// número de serie/garantía/costo, mientras que Device es la integración de software de control
// de acceso. Un mismo equipo físico puede tener ambos (ej. un Shelly es Device + InventoryItem),
// pero la mayoría del inventario (motores, cámaras sin integrar, cableado) no tiene Device asociado.
public class InventoryItem : AuditableEntity, ITenantOwned
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public InventoryCategory Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }

    public InventoryStatus Status { get; set; } = InventoryStatus.Active;
    public string? Location { get; set; }

    public DateTime? InstallationDate { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyUntil { get; set; }
    public string? Provider { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }

    // Vínculo opcional: si este activo físico también está integrado como Device de software.
    public Guid? LinkedDeviceId { get; set; }
    public Device? LinkedDevice { get; set; }
}
