using AccessControl.Application.Common.Exceptions;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Application.Services;

public interface IInventoryService
{
    Task<List<InventoryItemDto>> GetAllAsync(Guid? tenantId, CancellationToken ct = default);
    Task<InventoryItemDto> CreateAsync(InventoryItemCreateDto dto, CancellationToken ct = default);
    Task UpdateAsync(Guid id, InventoryItemUpdateDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

// Inventario de equipo físico por privada. SUPER_ADMIN puede ver/crear en cualquier privada
// (pasando TenantId explícito); ADMIN_PRIVADA queda siempre restringido a la suya propia
// por el Global Query Filter del DbContext, sin importar qué TenantId envíe en el DTO.
public class InventoryService : IInventoryService
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public InventoryService(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<InventoryItemDto>> GetAllAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var query = _db.InventoryItems.Include(i => i.Tenant).Include(i => i.LinkedDevice).AsQueryable();

        // Para SUPER_ADMIN, el filtro global no restringe nada: puede ver todas las privadas
        // o acotar a una en particular con ?tenantId=. Para ADMIN_PRIVADA, el filtro global
        // ya lo limita a la suya, así que este parámetro simplemente no aplica.
        if (_tenant.IsSuperAdmin && tenantId.HasValue)
            query = query.Where(i => i.TenantId == tenantId.Value);

        return await query.OrderBy(i => i.Tenant!.Name).ThenBy(i => i.Name)
            .Select(i => MapToDto(i))
            .ToListAsync(ct);
    }

    public async Task<InventoryItemDto> CreateAsync(InventoryItemCreateDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenant.IsSuperAdmin
            ? dto.TenantId ?? throw new ValidationAppException("Como SUPER_ADMIN debes indicar TenantId al crear un ítem de inventario.")
            : _tenant.TenantId!.Value;

        if (!await _db.Tenants.AnyAsync(t => t.Id == tenantId, ct))
            throw new NotFoundException(nameof(Tenant), tenantId);

        var item = new InventoryItem
        {
            TenantId = tenantId,
            Category = dto.Category,
            Name = dto.Name,
            Brand = dto.Brand,
            Model = dto.Model,
            SerialNumber = dto.SerialNumber,
            Location = dto.Location,
            InstallationDate = AsUtc(dto.InstallationDate),
            PurchaseDate = AsUtc(dto.PurchaseDate),
            WarrantyUntil = AsUtc(dto.WarrantyUntil),
            Provider = dto.Provider,
            Cost = dto.Cost,
            Notes = dto.Notes,
            LinkedDeviceId = dto.LinkedDeviceId
        };
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync(ct);

        var creado = await _db.InventoryItems.Include(i => i.Tenant).Include(i => i.LinkedDevice)
            .FirstAsync(i => i.Id == item.Id, ct);
        return MapToDto(creado);
    }

    public async Task UpdateAsync(Guid id, InventoryItemUpdateDto dto, CancellationToken ct = default)
    {
        var item = await _db.InventoryItems.FindAsync(new object?[] { id }, ct) ?? throw new NotFoundException(nameof(InventoryItem), id);

        item.Category = dto.Category;
        item.Name = dto.Name;
        item.Brand = dto.Brand;
        item.Model = dto.Model;
        item.SerialNumber = dto.SerialNumber;
        item.Status = dto.Status;
        item.Location = dto.Location;
        item.InstallationDate = AsUtc(dto.InstallationDate);
        item.PurchaseDate = AsUtc(dto.PurchaseDate);
        item.WarrantyUntil = AsUtc(dto.WarrantyUntil);
        item.Provider = dto.Provider;
        item.Cost = dto.Cost;
        item.Notes = dto.Notes;
        item.LinkedDeviceId = dto.LinkedDeviceId;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _db.InventoryItems.FindAsync(new object?[] { id }, ct) ?? throw new NotFoundException(nameof(InventoryItem), id);
        _db.InventoryItems.Remove(item);
        await _db.SaveChangesAsync(ct);
    }

    private static DateTime? AsUtc(DateTime? valor) =>
        valor.HasValue ? DateTime.SpecifyKind(valor.Value, DateTimeKind.Utc) : null;

    private static InventoryItemDto MapToDto(InventoryItem i) => new(
        i.Id, i.TenantId, i.Tenant?.Name ?? string.Empty, i.Category, i.Name, i.Brand, i.Model, i.SerialNumber,
        i.Status, i.Location, i.InstallationDate, i.PurchaseDate, i.WarrantyUntil, i.Provider, i.Cost, i.Notes,
        i.LinkedDeviceId, i.LinkedDevice?.Name);
}
