using AccessControl.Domain.Common;
using AccessControl.Domain.Enums;

namespace AccessControl.Domain.Entities;

public class Resident : AuditableEntity, ITenantOwned
{
    public Guid TenantId { get; set; }

    public Guid HouseId { get; set; }
    public House? House { get; set; }

    // Cuenta de acceso al sistema asociada (Identity). Es opcional: no todo residente
    // registrado necesita poder iniciar sesión (ej. hijos menores, familiares secundarios).
    public Guid? UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public ResidentStatus Status { get; set; } = ResidentStatus.Active;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
}
