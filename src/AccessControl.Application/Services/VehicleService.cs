using AccessControl.Application.Common.Exceptions;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Application.Services;

public interface IVehicleService
{
    Task<List<VehicleDto>> GetAllAsync(Guid? houseId, CancellationToken ct = default);
    Task<VehicleDto> CreateAsync(VehicleCreateDto dto, CancellationToken ct = default);
}

public class VehicleService : IVehicleService
{
    private readonly IAppDbContext _db;

    public VehicleService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<VehicleDto>> GetAllAsync(Guid? houseId, CancellationToken ct = default)
    {
        var query = _db.Vehicles.AsQueryable();
        if (houseId.HasValue) query = query.Where(v => v.HouseId == houseId.Value);

        return await query.OrderBy(v => v.Plate)
            .Select(v => new VehicleDto(v.Id, v.HouseId, v.Plate, v.Brand, v.Model, v.Color))
            .ToListAsync(ct);
    }

    public async Task<VehicleDto> CreateAsync(VehicleCreateDto dto, CancellationToken ct = default)
    {
        var house = await _db.Houses.FirstOrDefaultAsync(h => h.Id == dto.HouseId, ct)
            ?? throw new NotFoundException(nameof(House), dto.HouseId);

        var vehicle = new Vehicle
        {
            TenantId = house.TenantId,
            HouseId = dto.HouseId,
            Plate = dto.Plate,
            Brand = dto.Brand,
            Model = dto.Model,
            Color = dto.Color
        };
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync(ct);
        return new VehicleDto(vehicle.Id, vehicle.HouseId, vehicle.Plate, vehicle.Brand, vehicle.Model, vehicle.Color);
    }
}