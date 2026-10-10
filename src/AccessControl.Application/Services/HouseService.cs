using AccessControl.Application.Common;
using AccessControl.Application.Common.Exceptions;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Application.Services;

public interface IHouseService
{
    Task<List<HouseDto>> GetAllAsync(CancellationToken ct = default);
    Task<HouseDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<HouseDto> CreateAsync(HouseCreateDto dto, CancellationToken ct = default);
    Task UpdateAsync(Guid id, HouseUpdateDto dto, CancellationToken ct = default);
}

public class HouseService : IHouseService
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public HouseService(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<HouseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var consulta = _db.Houses.AsQueryable();
        if (_tenant.EsResidente) consulta = consulta.Where(h => h.Residents.Any(r => r.UserId == _tenant.UserId));

        var casas = await consulta
            .OrderBy(h => h.Number)
            .Select(h => new { h.Id, h.Number, h.Notes, h.Status, Total = h.Residents.Count, h.TenantId })
            .ToListAsync(ct);
        var nombres = await TenantScope.NombresAsync(_db, casas.Select(c => c.TenantId), ct);
        return casas.Select(c => new HouseDto(c.Id, c.Number, c.Notes, c.Status, c.Total, c.TenantId, nombres.GetValueOrDefault(c.TenantId, ""))).ToList();
    }

    public async Task<HouseDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var house = await _db.Houses.Include(h => h.Residents)
            .Where(h => !_tenant.EsResidente || h.Residents.Any(r => r.UserId == _tenant.UserId))
            .FirstOrDefaultAsync(h => h.Id == id, ct)
            ?? throw new NotFoundException(nameof(House), id);
        var nombres = await TenantScope.NombresAsync(_db, new[] { house.TenantId }, ct);
        return new HouseDto(house.Id, house.Number, house.Notes, house.Status, house.Residents.Count, house.TenantId, nombres.GetValueOrDefault(house.TenantId, ""));
    }

    public async Task<HouseDto> CreateAsync(HouseCreateDto dto, CancellationToken ct = default)
    {
        var tenantId = await TenantScope.ResolverAsync(_db, _tenant, dto.TenantId, ct);
        var house = new House
        {
            TenantId = tenantId,
            Number = dto.Number,
            Notes = dto.Notes
        };
        _db.Houses.Add(house);
        await _db.SaveChangesAsync(ct);
        var nombres = await TenantScope.NombresAsync(_db, new[] { tenantId }, ct);
        return new HouseDto(house.Id, house.Number, house.Notes, house.Status, 0, tenantId, nombres.GetValueOrDefault(tenantId, ""));
    }

    public async Task UpdateAsync(Guid id, HouseUpdateDto dto, CancellationToken ct = default)
    {
        var house = await _db.Houses.FindAsync(new object?[] { id }, ct) ?? throw new NotFoundException(nameof(House), id);
        house.Number = dto.Number;
        house.Notes = dto.Notes;
        house.Status = dto.Status;
        house.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}