using AccessControl.Domain.Common;
using AccessControl.Domain.Enums;

namespace AccessControl.Domain.Entities;

// Auditoría de todo intento relevante del sistema: escaneos, autorizaciones, rechazos,
// comandos de apertura y sus resultados, aperturas manuales, revocaciones, expiraciones.
public class AccessEvent : AuditableEntity, ITenantOwned
{
    public Guid TenantId { get; set; }

    public Guid? AccessPointId { get; set; }
    public AccessPoint? AccessPoint { get; set; }

    public Guid? DeviceId { get; set; }
    public Device? Device { get; set; }

    public Guid? AccessPassId { get; set; }
    public AccessPass? AccessPass { get; set; }

    // Usuario relacionado con el evento cuando aplica (guardia que hizo apertura manual, etc.)
    public Guid? UserId { get; set; }

    public AccessEventType EventType { get; set; }
    public AccessEventResult Result { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }

    // Detalles adicionales (motivo de rechazo, respuesta cruda del driver de hardware, etc.)
    public string? Metadata { get; set; }
}
