using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Application.Services;

public interface IAccessValidationService
{
    Task<ValidateTokenResponseDto> ValidatePreviewAsync(string token, CancellationToken ct = default);
    Task<ScanResponseDto> ScanAsync(ScanRequestDto request, Guid? guardUserId, string? ipAddress, CancellationToken ct = default);
}

// El validate es de solo lectura (semáforo en la app del guardia); el scan es el único
// autoritativo: vuelve a validar desde cero, consume el uso de forma atómica y dispara la
// apertura física. La privada de un escaneo siempre es la del pase; un superadmin no tiene
// privada propia, así que las puertas se buscan dentro de la privada del pase.
public class AccessValidationService : IAccessValidationService
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ISecureTokenService _tokens;
    private readonly IAccessControllerFactory _controllerFactory;

    public AccessValidationService(
        IAppDbContext db, ITenantContext tenant, ISecureTokenService tokens, IAccessControllerFactory controllerFactory)
    {
        _db = db;
        _tenant = tenant;
        _tokens = tokens;
        _controllerFactory = controllerFactory;
    }

    public async Task<ValidateTokenResponseDto> ValidatePreviewAsync(string token, CancellationToken ct = default)
    {
        var (pass, denialReason) = await LookupAndCheckAsync(token, ct);
        if (pass is null)
            return new ValidateTokenResponseDto(false, denialReason, null, null, null);

        var house = await _db.Houses.FindAsync(new object?[] { pass.HouseId }, ct);
        return new ValidateTokenResponseDto(true, null, pass.VisitorName, house?.Number, pass.VehiclePlate);
    }

    public async Task<ScanResponseDto> ScanAsync(ScanRequestDto request, Guid? guardUserId, string? ipAddress, CancellationToken ct = default)
    {
        await LogEventAsync(null, null, null, null, AccessEventType.QrScanned, AccessEventResult.CommandSent, guardUserId, ipAddress, null, ct);

        var (pass, denialReason) = await LookupAndCheckAsync(request.Token, ct);
        if (pass is null)
        {
            await LogEventAsync(null, null, null, null, AccessEventType.AccessDenied, AccessEventResult.Denied, guardUserId, ipAddress, denialReason, ct);
            return new ScanResponseDto(false, denialReason, false, null, null, null);
        }

        var tenantId = pass.TenantId;
        var house = await _db.Houses.FindAsync(new object?[] { pass.HouseId }, ct);

        // Consumo atómico del uso: evita que dos escaneos simultáneos agoten un pase de un solo uso dos veces.
        var filas = await _db.AccessPasses
            .Where(p => p.Id == pass.Id && p.CurrentUses < p.MaxUses && p.Status == AccessPassStatus.Active)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.CurrentUses, p => p.CurrentUses + 1)
                .SetProperty(p => p.Status, p => p.CurrentUses + 1 >= p.MaxUses ? AccessPassStatus.Used : AccessPassStatus.Active), ct);

        if (filas == 0)
        {
            await LogEventAsync(tenantId, null, null, pass.Id, AccessEventType.AccessDenied, AccessEventResult.Denied, guardUserId, ipAddress, "Sin usos disponibles (condición de carrera o ya agotado).", ct);
            return new ScanResponseDto(false, "El pase ya no tiene usos disponibles.", false, pass.VisitorName, house?.Number, pass.VehiclePlate);
        }

        await LogEventAsync(tenantId, null, null, pass.Id, AccessEventType.AccessAuthorized, AccessEventResult.Authorized, guardUserId, ipAddress, null, ct);

        var accessPoint = request.AccessPointId.HasValue
            ? await _db.AccessPoints.FirstOrDefaultAsync(a => a.Id == request.AccessPointId.Value && a.IsActive && a.TenantId == tenantId, ct)
            : await _db.AccessPoints.FirstOrDefaultAsync(a => a.IsActive && a.TenantId == tenantId, ct);

        if (accessPoint is null)
        {
            await DevolverUsoAsync(pass.Id, ct);
            await LogEventAsync(tenantId, null, null, pass.Id, AccessEventType.OpenCommandFailed, AccessEventResult.Failed, guardUserId, ipAddress, "No hay AccessPoint activo configurado.", ct);
            return new ScanResponseDto(true, null, false, pass.VisitorName, house?.Number, pass.VehiclePlate);
        }

        var link = await _db.DeviceAccessPoints
            .Include(l => l.Device)
            .Where(l => l.AccessPointId == accessPoint.Id)
            .FirstOrDefaultAsync(ct);

        if (link?.Device is null)
        {
            await DevolverUsoAsync(pass.Id, ct);
            await LogEventAsync(tenantId, accessPoint.Id, null, pass.Id, AccessEventType.OpenCommandFailed, AccessEventResult.Failed, guardUserId, ipAddress, "El AccessPoint no tiene ningún Device vinculado.", ct);
            return new ScanResponseDto(true, null, false, pass.VisitorName, house?.Number, pass.VehiclePlate);
        }

        var controller = _controllerFactory.Resolve(link.Device);
        var openResult = await controller.OpenAsync(link.Device, new OpenRequest(link.ChannelNumber, null, guardUserId, null), ct);

        if (!openResult.CommandAccepted)
            await DevolverUsoAsync(pass.Id, ct);

        await LogEventAsync(
            tenantId, accessPoint.Id, link.Device.Id, pass.Id,
            openResult.CommandAccepted ? AccessEventType.OpenCommandSent : AccessEventType.OpenCommandFailed,
            openResult.CommandAccepted ? AccessEventResult.CommandSent : AccessEventResult.Failed,
            guardUserId, ipAddress, openResult.RawProviderResponse ?? openResult.ErrorMessage, ct);

        return new ScanResponseDto(true, null, openResult.CommandAccepted, pass.VisitorName, house?.Number, pass.VehiclePlate);
    }

    // Si la pluma no abrió, el visitante no entró: el uso consumido se devuelve para que el guardia pueda reintentar.
    private async Task DevolverUsoAsync(Guid passId, CancellationToken ct)
    {
        await _db.AccessPasses
            .Where(p => p.Id == passId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.CurrentUses, p => p.CurrentUses - 1)
                .SetProperty(p => p.Status, AccessPassStatus.Active), ct);
    }

    private async Task<(AccessPass? Pass, string? DenialReason)> LookupAndCheckAsync(string rawToken, CancellationToken ct)
    {
        if (!_tokens.TryParseToken(rawToken, out var selectorId, out var verifier))
            return (null, "Token con formato inválido.");

        var pass = await _db.AccessPasses.FirstOrDefaultAsync(p => p.SelectorId == selectorId, ct);
        if (pass is null)
            return (null, "Token no encontrado.");

        if (!_tokens.VerifyHash(verifier, pass.VerifierHash))
            return (null, "Token inválido.");

        if (pass.Status is AccessPassStatus.Revoked)
            return (null, "El pase fue revocado.");
        if (pass.Status is AccessPassStatus.Disabled)
            return (null, "El pase está deshabilitado.");
        if (pass.Status is AccessPassStatus.Used)
            return (null, "El pase ya fue utilizado.");

        var now = DateTime.UtcNow;
        if (now < pass.ValidFrom || now > pass.ValidUntil)
            return (null, "El pase está fuera de su ventana de vigencia.");

        if (pass.CurrentUses >= pass.MaxUses)
            return (null, "El pase ya no tiene usos disponibles.");

        return (pass, null);
    }

    // Sin privada conocida (token inexistente escaneado por un superadmin) no hay a qué
    // privada asociar el evento, así que no se registra.
    private async Task LogEventAsync(
        Guid? tenantId, Guid? accessPointId, Guid? deviceId, Guid? accessPassId, AccessEventType type, AccessEventResult result,
        Guid? userId, string? ipAddress, string? metadata, CancellationToken ct)
    {
        var tenant = tenantId ?? _tenant.TenantId;
        if (tenant is null) return;

        _db.AccessEvents.Add(new AccessEvent
        {
            TenantId = tenant.Value,
            AccessPointId = accessPointId,
            DeviceId = deviceId,
            AccessPassId = accessPassId,
            UserId = userId,
            EventType = type,
            Result = result,
            IpAddress = ipAddress,
            Metadata = metadata
        });
        await _db.SaveChangesAsync(ct);
    }
}
