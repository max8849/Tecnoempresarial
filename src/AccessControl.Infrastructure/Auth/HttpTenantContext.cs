using AccessControl.Application.Common.Interfaces;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;

namespace AccessControl.Infrastructure.Auth;

// Resuelve el tenant/usuario actual a partir de los claims del JWT de la request en curso.
// Es el único lugar donde "quién soy y de qué privada soy" se traduce a algo que el
// resto de la aplicación (DbContext incluido) puede usar sin saber nada de HTTP/JWT.
public class HttpTenantContext : ITenantContext
{
    public Guid? TenantId { get; }
    public Guid? UserId { get; }
    public bool IsSuperAdmin { get; }
    public bool EsResidente { get; }

    public HttpTenantContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return;

        var tenantClaim = user.FindFirst("tenantId")?.Value;
        if (Guid.TryParse(tenantClaim, out var tenantId)) TenantId = tenantId;

        var subClaim = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (Guid.TryParse(subClaim, out var userId)) UserId = userId;

        IsSuperAdmin = user.IsInRole(AppRoles.SuperAdmin);
        EsResidente = user.IsInRole(AppRoles.Residente) && !user.IsInRole(AppRoles.AdminPrivada);
    }
}
