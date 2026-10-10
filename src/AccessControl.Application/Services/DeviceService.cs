using System.Text.Json;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Exceptions;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Application.Services;

public interface IDeviceService
{
    Task<List<DeviceDto>> GetAllAsync(CancellationToken ct = default);
    Task<DeviceDto> CreateAsync(DeviceCreateDto dto, CancellationToken ct = default);
    Task LinkAccessPointAsync(Guid deviceId, DeviceLinkAccessPointDto dto, CancellationToken ct = default);
    Task<DeviceTestConnectionResultDto> TestConnectionAsync(Guid deviceId, CancellationToken ct = default);
}

public class DeviceService : IDeviceService
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAccessControllerFactory _controllerFactory;

    public DeviceService(IAppDbContext db, ITenantContext tenant, IAccessControllerFactory controllerFactory)
    {
        _db = db;
        _tenant = tenant;
        _controllerFactory = controllerFactory;
    }

    public async Task<List<DeviceDto>> GetAllAsync(CancellationToken ct = default)
    {
        var devices = await _db.Devices
            .Include(d => d.AccessPointLinks).ThenInclude(l => l.AccessPoint)
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

        var nombres = await TenantScope.NombresAsync(_db, devices.Select(d => d.TenantId), ct);
        return devices.Select(d => MapToDto(d, nombres)).ToList();
    }

    public async Task<DeviceDto> CreateAsync(DeviceCreateDto dto, CancellationToken ct = default)
    {
        var tenantId = await TenantScope.ResolverAsync(_db, _tenant, dto.TenantId, ct);
        var device = new Device
        {
            TenantId = tenantId,
            Name = dto.Name,
            Manufacturer = dto.Manufacturer,
            Model = dto.Model,
            IpAddress = dto.IpAddress,
            DriverType = dto.DriverType,
            ConfigurationJson = ArmarConfiguracion(dto)
        };
        _db.Devices.Add(device);
        await _db.SaveChangesAsync(ct);
        var nombres = await TenantScope.NombresAsync(_db, new[] { tenantId }, ct);
        return MapToDto(device, nombres);
    }

    public async Task LinkAccessPointAsync(Guid deviceId, DeviceLinkAccessPointDto dto, CancellationToken ct = default)
    {
        var device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == deviceId, ct)
            ?? throw new NotFoundException(nameof(Device), deviceId);
        var punto = await _db.AccessPoints.FirstOrDefaultAsync(a => a.Id == dto.AccessPointId, ct)
            ?? throw new NotFoundException(nameof(AccessPoint), dto.AccessPointId);

        if (punto.TenantId != device.TenantId)
            throw new ValidationAppException("El punto de acceso y el dispositivo deben pertenecer a la misma privada.");

        _db.DeviceAccessPoints.Add(new DeviceAccessPoint
        {
            DeviceId = deviceId,
            AccessPointId = dto.AccessPointId,
            ChannelNumber = dto.ChannelNumber
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<DeviceTestConnectionResultDto> TestConnectionAsync(Guid deviceId, CancellationToken ct = default)
    {
        var device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == deviceId, ct)
            ?? throw new NotFoundException(nameof(Device), deviceId);

        var controller = _controllerFactory.Resolve(device);
        var resultado = await controller.ValidateConnectionAsync(device, ct);

        device.Status = resultado.IsReachable ? DeviceStatus.Online : DeviceStatus.Offline;
        await _db.SaveChangesAsync(ct);

        return new DeviceTestConnectionResultDto(resultado.IsReachable, resultado.Details);
    }

    // Solo se arma configuración estructurada si mandaron usuario/contraseña (drivers como
    // Hikvision que hablan HTTP a un panel real). Mock no necesita nada de esto.
    private static string? ArmarConfiguracion(DeviceCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return null;

        return JsonSerializer.Serialize(new
        {
            username = dto.Username,
            password = dto.Password,
            port = dto.Port ?? 80,
            useHttps = dto.UseHttps ?? false
        });
    }

    private static DeviceDto MapToDto(Device d, Dictionary<Guid, string> nombres) => new(
        d.Id, d.Name, d.Manufacturer, d.Model, d.IpAddress, d.Status, d.DriverType,
        !string.IsNullOrWhiteSpace(d.ConfigurationJson),
        d.AccessPointLinks.Select(l => new DeviceAccessPointDto(l.AccessPointId, l.AccessPoint?.Name ?? string.Empty, l.ChannelNumber)).ToList(),
        d.TenantId, nombres.GetValueOrDefault(d.TenantId, ""));
}