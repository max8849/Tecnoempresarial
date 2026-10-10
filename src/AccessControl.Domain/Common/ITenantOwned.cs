namespace AccessControl.Domain.Common;

// Marca cualquier entidad cuyo acceso deba quedar aislado por privada (tenant).
// La Infrastructure usa esto para aplicar el Global Query Filter automáticamente a todas.
public interface ITenantOwned
{
    Guid TenantId { get; set; }
}
