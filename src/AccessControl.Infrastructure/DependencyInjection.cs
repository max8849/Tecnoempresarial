using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.Services;
using AccessControl.Infrastructure.Auth;
using AccessControl.Infrastructure.HardwareAdapters;
using AccessControl.Infrastructure.Identity;
using AccessControl.Infrastructure.Persistence;
using AccessControl.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccessControl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();

        var provider = config["Database:Provider"] ?? "Postgres";
        services.AddDbContext<AppDbContext>(options =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(config.GetConnectionString("Sqlite"));
            else
                options.UseNpgsql(config.GetConnectionString("Default"));
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredLength = 10;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ISecureTokenService, SecureTokenService>();
        services.AddScoped<IQrImageGenerator, QrImageGenerator>();

        services.AddScoped<MockAccessController>();
        services.AddScoped<HikvisionAccessController>();
        services.AddScoped<RemoteAgentAccessController>();
        services.AddScoped<IAccessControllerFactory, AccessControllerFactory>();

        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IHouseService, HouseService>();
        services.AddScoped<IResidentService, ResidentService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IAccessPointService, AccessPointService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IAccessPassService, AccessPassService>();
        services.AddScoped<IAccessValidationService, AccessValidationService>();
        services.AddScoped<IAccessEventService, AccessEventService>();
        services.AddScoped<IInventoryService, InventoryService>();

        return services;
    }
}
