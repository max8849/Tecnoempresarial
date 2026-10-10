using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Application.Common.Interfaces;

// Abstracción mínima sobre el DbContext para que Application no dependa del proveedor
// real de EF Core (Npgsql/Sqlite viven en Infrastructure). Nada de repositorios genéricos:
// los servicios de Application consultan estos DbSet directamente con LINQ.
public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<House> Houses { get; }
    DbSet<Resident> Residents { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<AccessPass> AccessPasses { get; }
    DbSet<AccessPoint> AccessPoints { get; }
    DbSet<Device> Devices { get; }
    DbSet<DeviceAccessPoint> DeviceAccessPoints { get; }
    DbSet<AccessEvent> AccessEvents { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<InventoryItem> InventoryItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
