namespace AccessControl.Application.Common.Interfaces;

// Resuelto por la Api a partir de los claims del JWT en cada request, e inyectado
// como scoped service. Infrastructure lo usa para armar los Global Query Filters de EF Core.
public interface ITenantContext
{
    Guid? TenantId { get; }
    Guid? UserId { get; }
    bool IsSuperAdmin { get; }
    bool EsResidente { get; }
}
