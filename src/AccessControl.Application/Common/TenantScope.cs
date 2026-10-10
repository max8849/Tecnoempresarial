using AccessControl.Application.Common.Exceptions;
using AccessControl.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Application.Common;

public static class TenantScope
{
    public static async Task<Guid> ResolverAsync(IAppDbContext db, ITenantContext tenant, Guid? tenantIdSolicitado, CancellationToken ct = default)
    {
        if (!tenant.IsSuperAdmin)
            return tenant.TenantId ?? throw new ForbiddenException();

        if (tenantIdSolicitado is null)
            throw new ValidationAppException("Indica la privada a la que pertenece este registro.");

        if (!await db.Tenants.AnyAsync(t => t.Id == tenantIdSolicitado.Value, ct))
            throw new NotFoundException("Tenant", tenantIdSolicitado.Value);

        return tenantIdSolicitado.Value;
    }

    public static async Task<Dictionary<Guid, string>> NombresAsync(IAppDbContext db, IEnumerable<Guid> tenantIds, CancellationToken ct = default)
    {
        var ids = tenantIds.Distinct().ToList();
        return await db.Tenants.Where(t => ids.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name, ct);
    }
}
