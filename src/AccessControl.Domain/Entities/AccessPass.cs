using AccessControl.Domain.Common;
using AccessControl.Domain.Enums;

namespace AccessControl.Domain.Entities;

// El QR nunca contiene datos del visitante ni de la casa: solo codifica "SelectorId.Verifier".
// Se persiste el hash del Verifier (VerifierHash) para validarlo en el escaneo, y también
// el contenido completo en QrContent únicamente para poder volver a mostrar la imagen QR
// mientras el pase siga Activo (el residente puede querer reenviarla). Es una concesión
// consciente a la regla original de "el verifier nunca se guarda en claro".
public class AccessPass : AuditableEntity, ITenantOwned
{
    public Guid TenantId { get; set; }

    public Guid HouseId { get; set; }
    public House? House { get; set; }

    public Guid CreatedByUserId { get; set; }

    public string VisitorName { get; set; } = string.Empty;
    public string? VehiclePlate { get; set; }
    public string? Notes { get; set; }

    // Parte pública del token: identifica el registro rápidamente (indexado, no es secreto).
    public string SelectorId { get; set; } = string.Empty;

    // Hash (con pepper del servidor) de la parte secreta del token, usado para validar el escaneo.
    public string VerifierHash { get; set; } = string.Empty;

    // Contenido completo "selector.verifier" para poder regenerar la imagen QR mientras
    // el pase esté Activo. Ver nota de la clase sobre la contrapartida de seguridad.
    public string? QrContent { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }

    public int MaxUses { get; set; } = 1;
    public int CurrentUses { get; set; }

    public AccessPassStatus Status { get; set; } = AccessPassStatus.Active;

    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedByUserId { get; set; }

    public ICollection<AccessEvent> AccessEvents { get; set; } = new List<AccessEvent>();
}
