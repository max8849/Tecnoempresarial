using Microsoft.AspNetCore.Identity;

namespace AccessControl.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    // Null = SUPER_ADMIN (alcance global). Con valor = usuario restringido a ese tenant/privada.
    public Guid? TenantId { get; set; }
}

public static class AppRoles
{
    public const string SuperAdmin = "SUPER_ADMIN";
    public const string AdminPrivada = "ADMIN_PRIVADA";
    public const string Residente = "RESIDENTE";
    public const string Guardia = "GUARDIA";
    public const string Operador = "OPERADOR";

    // Rol técnico: lo usa exclusivamente el programa Agente al conectarse al Hub de
    // SignalR. Nunca lo tiene un usuario humano ni aparece en AspNetUsers.
    public const string AgenteLocal = "AGENTE_LOCAL";
}
