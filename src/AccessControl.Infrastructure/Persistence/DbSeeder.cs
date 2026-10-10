using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccessControl.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var db = services.GetRequiredService<AppDbContext>();
        if (db.Database.IsSqlite())
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { AppRoles.SuperAdmin, AppRoles.AdminPrivada, AppRoles.Residente, AppRoles.Guardia, AppRoles.Operador, AppRoles.AgenteLocal })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // SUPER_ADMIN (sin TenantId: ve todas las privadas)
        var superAdminEmail = config["Seed:SuperAdminEmail"] ?? "superadmin@accesscontrol.local";
        if (await userManager.FindByEmailAsync(superAdminEmail) is null)
        {
            var superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail, Email = superAdminEmail, FullName = "Super Administrador", EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(superAdmin, config["Seed:SuperAdminPassword"] ?? "SuperAdmin123!");
            if (result.Succeeded) await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);
        }

        // Tenant de demostración con datos de ejemplo (privada, casas, punto de acceso, device Mock)
        if (!await db.Tenants.AnyAsync())
        {
            var tenant = new Tenant { Name = "Privada Las Palmas", Slug = "las-palmas" };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var casas = new List<House>();
            for (int i = 1; i <= 5; i++)
                casas.Add(new House { TenantId = tenant.Id, Number = $"Casa {i}" });
            db.Houses.AddRange(casas);

            var accessPoint = new AccessPoint
            {
                TenantId = tenant.Id, Name = "Entrada Principal", Type = AccessPointType.VehicularGate, Direction = AccessDirection.In
            };
            db.AccessPoints.Add(accessPoint);

            var device = new Device
            {
                TenantId = tenant.Id, Name = "Pluma Entrada Principal", Manufacturer = "Mock",
                DriverType = DriverType.Mock, Status = DeviceStatus.Online
            };
            db.Devices.Add(device);
            await db.SaveChangesAsync();

            db.DeviceAccessPoints.Add(new DeviceAccessPoint { DeviceId = device.Id, AccessPointId = accessPoint.Id, ChannelNumber = 1 });
            await db.SaveChangesAsync();

            db.InventoryItems.AddRange(
                new InventoryItem
                {
                    TenantId = tenant.Id, Category = InventoryCategory.Barrier, Name = "Pluma Entrada Principal",
                    Brand = "CAME", Model = "Gard 4", SerialNumber = "CAME-2024-0001",
                    Location = "Caseta principal", LinkedDeviceId = device.Id
                },
                new InventoryItem
                {
                    TenantId = tenant.Id, Category = InventoryCategory.HikvisionPanel, Name = "Panel de control caseta",
                    Brand = "Hikvision", Model = "Por definir", SerialNumber = "PENDIENTE",
                    Location = "Caseta principal", Notes = "Modelo exacto pendiente de confirmar en sitio."
                },
                new InventoryItem
                {
                    TenantId = tenant.Id, Category = InventoryCategory.Camera, Name = "Cámara entrada principal",
                    Brand = "Hikvision", Model = "DS-2CD2043G2", SerialNumber = "HK-CAM-0001",
                    Location = "Caseta principal"
                });
            await db.SaveChangesAsync();

            // ADMIN_PRIVADA de esa privada
            var adminEmail = config["Seed:AdminPrivadaEmail"] ?? "admin@laspalmas.local";
            if (await userManager.FindByEmailAsync(adminEmail) is null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail, Email = adminEmail, FullName = "Admin Las Palmas",
                    EmailConfirmed = true, TenantId = tenant.Id
                };
                var result = await userManager.CreateAsync(admin, config["Seed:AdminPrivadaPassword"] ?? "Admin123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(admin, AppRoles.AdminPrivada);
            }

            // RESIDENTE de ejemplo en Casa 1
            var residenteEmail = config["Seed:ResidenteEmail"] ?? "residente@laspalmas.local";
            if (await userManager.FindByEmailAsync(residenteEmail) is null)
            {
                var residenteUser = new ApplicationUser
                {
                    UserName = residenteEmail, Email = residenteEmail, FullName = "Juan Residente",
                    EmailConfirmed = true, TenantId = tenant.Id
                };
                var result = await userManager.CreateAsync(residenteUser, config["Seed:ResidentePassword"] ?? "Residente123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(residenteUser, AppRoles.Residente);
                    db.Residents.Add(new Resident
                    {
                        TenantId = tenant.Id, HouseId = casas[0].Id, UserId = residenteUser.Id,
                        FirstName = "Juan", LastName = "Residente"
                    });
                }
            }

            // GUARDIA de ejemplo
            var guardiaEmail = config["Seed:GuardiaEmail"] ?? "guardia@laspalmas.local";
            if (await userManager.FindByEmailAsync(guardiaEmail) is null)
            {
                var guardia = new ApplicationUser
                {
                    UserName = guardiaEmail, Email = guardiaEmail, FullName = "Guardia Caseta 1",
                    EmailConfirmed = true, TenantId = tenant.Id
                };
                var result = await userManager.CreateAsync(guardia, config["Seed:GuardiaPassword"] ?? "Guardia123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(guardia, AppRoles.Guardia);
            }

            await db.SaveChangesAsync();
        }
    }
}
