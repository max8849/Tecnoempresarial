using AccessControl.Application.Common;
using AccessControl.Application.Common.Exceptions;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Application.Services;

public interface IResidentService
{
    Task<List<ResidentDto>> GetAllAsync(Guid? houseId, CancellationToken ct = default);
    Task<ResidentDto> CreateAsync(ResidentCreateDto dto, CancellationToken ct = default);
    Task UpdateAsync(Guid id, ResidentUpdateDto dto, CancellationToken ct = default);
}

public class ResidentService : IResidentService
{
    private readonly IAppDbContext _db;

    public ResidentService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ResidentDto>> GetAllAsync(Guid? houseId, CancellationToken ct = default)
    {
        var query = _db.Residents.Include(r => r.House).AsQueryable();
        if (houseId.HasValue) query = query.Where(r => r.HouseId == houseId.Value);

        var residentes = await query.OrderBy(r => r.LastName).ToListAsync(ct);
        var nombres = await TenantScope.NombresAsync(_db, residentes.Select(r => r.TenantId), ct);
        return residentes.Select(r => Mapear(r, r.House!.Number, nombres)).ToList();
    }

    public async Task<ResidentDto> CreateAsync(ResidentCreateDto dto, CancellationToken ct = default)
    {
        var house = await _db.Houses.FirstOrDefaultAsync(h => h.Id == dto.HouseId, ct)
            ?? throw new NotFoundException(nameof(House), dto.HouseId);

        var resident = new Resident
        {
            TenantId = house.TenantId,
            HouseId = dto.HouseId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            Email = dto.Email
        };
        _db.Residents.Add(resident);
        await _db.SaveChangesAsync(ct);
        var nombres = await TenantScope.NombresAsync(_db, new[] { house.TenantId }, ct);
        return Mapear(resident, house.Number, nombres);
    }

    public async Task UpdateAsync(Guid id, ResidentUpdateDto dto, CancellationToken ct = default)
    {
        var resident = await _db.Residents.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Resident), id);

        resident.FirstName = dto.FirstName;
        resident.LastName = dto.LastName;
        resident.Phone = dto.Phone;
        resident.Email = dto.Email;
        resident.Status = dto.Status;
        resident.EndDate = dto.EndDate;
        resident.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static ResidentDto Mapear(Resident r, string numeroCasa, Dictionary<Guid, string> nombres) => new(
        r.Id, r.HouseId, numeroCasa, r.FirstName, r.LastName, r.Phone, r.Email, r.Status, r.StartDate, r.EndDate, r.TenantId,
        nombres.GetValueOrDefault(r.TenantId, ""));
}