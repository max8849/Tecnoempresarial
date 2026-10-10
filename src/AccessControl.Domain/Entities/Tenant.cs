using AccessControl.Domain.Common;

namespace AccessControl.Domain.Entities;

// Un Tenant = una privada residencial. Cada cliente del SaaS es un Tenant.
public class Tenant : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Clave secreta que usa el Agente local de esta privada para autenticarse y obtener
    // su propio JWT (rol AGENTE_LOCAL) antes de conectarse al Hub de SignalR.
    public string AgentApiKey { get; set; } = Guid.NewGuid().ToString("N");

    public ICollection<House> Houses { get; set; } = new List<House>();
}
