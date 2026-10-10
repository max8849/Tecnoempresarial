using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IAppDbContext
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Referenciadas como miembros de instancia (no variables capturadas) para que EF Core
    // reevalúe el filtro con el tenant correcto en cada request, no con un valor "congelado"
    // del momento en que se construyó el modelo la primera vez.
    private Guid? CurrentTenantId => _tenantContext.TenantId;
    private bool IsSuperAdmin => _tenantContext.IsSuperAdmin;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<House> Houses => Set<House>();
    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<AccessPass> AccessPasses => Set<AccessPass>();
    public DbSet<AccessPoint> AccessPoints => Set<AccessPoint>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceAccessPoint> DeviceAccessPoints => Set<DeviceAccessPoint>();
    public DbSet<AccessEvent> AccessEvents => Set<AccessEvent>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<House>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        builder.Entity<Resident>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        builder.Entity<Vehicle>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        builder.Entity<AccessPass>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        builder.Entity<AccessPoint>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        builder.Entity<Device>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        builder.Entity<AccessEvent>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);
        builder.Entity<InventoryItem>().HasQueryFilter(e => IsSuperAdmin || e.TenantId == CurrentTenantId);

        builder.Entity<House>().HasIndex(h => new { h.TenantId, h.Number }).IsUnique();

        builder.Entity<AccessPass>().HasIndex(p => p.SelectorId).IsUnique();
        builder.Entity<AccessPass>().HasIndex(p => p.TenantId);

        builder.Entity<DeviceAccessPoint>().HasKey(l => new { l.DeviceId, l.AccessPointId });
        builder.Entity<DeviceAccessPoint>()
            .HasOne(l => l.Device).WithMany(d => d.AccessPointLinks).HasForeignKey(l => l.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<DeviceAccessPoint>()
            .HasOne(l => l.AccessPoint).WithMany(a => a.DeviceLinks).HasForeignKey(l => l.AccessPointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<House>().HasOne(h => h.Tenant).WithMany(t => t.Houses).HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Resident>().HasOne(r => r.House).WithMany(h => h.Residents).HasForeignKey(r => r.HouseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Vehicle>().HasOne(v => v.House).WithMany(h => h.Vehicles).HasForeignKey(v => v.HouseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AccessPass>().HasOne(p => p.House).WithMany(h => h.AccessPasses).HasForeignKey(p => p.HouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AccessEvent>().HasOne(e => e.AccessPoint).WithMany().HasForeignKey(e => e.AccessPointId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<AccessEvent>().HasOne(e => e.Device).WithMany().HasForeignKey(e => e.DeviceId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<AccessEvent>().HasOne(e => e.AccessPass).WithMany(p => p.AccessEvents).HasForeignKey(e => e.AccessPassId)
            .OnDelete(DeleteBehavior.SetNull);

        foreach (var enumProperty in new[]
        {
            builder.Entity<House>().Property(h => h.Status),
        })
        {
            enumProperty.HasConversion<string>();
        }

        builder.Entity<Resident>().Property(r => r.Status).HasConversion<string>();
        builder.Entity<AccessPass>().Property(p => p.Status).HasConversion<string>();
        builder.Entity<AccessPoint>().Property(a => a.Type).HasConversion<string>();
        builder.Entity<AccessPoint>().Property(a => a.Direction).HasConversion<string>();
        builder.Entity<Device>().Property(d => d.Status).HasConversion<string>();
        builder.Entity<Device>().Property(d => d.DriverType).HasConversion<string>();
        builder.Entity<AccessEvent>().Property(e => e.EventType).HasConversion<string>();
        builder.Entity<AccessEvent>().Property(e => e.Result).HasConversion<string>();

        builder.Entity<InventoryItem>().Property(i => i.Category).HasConversion<string>();
        builder.Entity<InventoryItem>().Property(i => i.Status).HasConversion<string>();
        builder.Entity<InventoryItem>().Property(i => i.Cost).HasPrecision(12, 2);
        builder.Entity<InventoryItem>()
            .HasOne(i => i.LinkedDevice).WithMany().HasForeignKey(i => i.LinkedDeviceId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<InventoryItem>()
            .HasOne(i => i.Tenant).WithMany().HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
