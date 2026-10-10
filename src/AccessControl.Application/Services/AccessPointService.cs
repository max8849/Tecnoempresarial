using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Application.Services;

public interface IAccessPointService
{
    Task<List<AccessPointDto>> GetAllAsync(CancellationToken ct = default);
    Task<AccessPointDto> CreateAsync(AccessPointCreateDto dto, CancellationToken ct = default);
}

public class AccessPointService : IAccessPointService
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AccessPointService(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<AccessPointDto>> GetAllAsync(CancellationToken ct = default)
    {
        var puntos = await _db.AccessPoints.OrderBy(a => a.Name).ToListAsync(ct);
        var nombres = await TenantScope.NombresAsync(_db, puntos.Select(p => p.TenantId), ct);
        return puntos.Select(a => new AccessPointDto(a.Id, a.Name, a.Type, a.Direction, a.IsActive, a.TenantId, nombres.GetValueOrDefault(a.TenantId, ""))).ToList();
    }

    public async Task<AccessPointDto> CreateAsync(AccessPointCreateDto dto, CancellationToken ct = default)
    {
        var tenantId = await TenantScope.ResolverAsync(_db, _tenant, dto.TenantId, ct);
        var point = new AccessPoint
        {
            TenantId = tenantId,
            Name = dto.Name,
            Type = dto.Type,
            Direction = dto.Direction
        };
        _db.AccessPoints.Add(point);
        await _db.SaveChangesAsync(ct);
        var nombres = await TenantScope.NombresAsync(_db, new[] { tenantId }, ct);
        return new AccessPointDto(point.Id, point.Name, point.Type, point.Direction, point.IsActive, tenantId, nombres.GetValueOrDefault(tenantId, ""));
    }
}