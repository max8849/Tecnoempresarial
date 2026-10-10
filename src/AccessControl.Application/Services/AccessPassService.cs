using AccessControl.Application.Common.Exceptions;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Application.Services;

public interface IAccessPassService
{
    Task<List<AccessPassDto>> GetAllAsync(Guid? houseId, CancellationToken ct = default);
    Task<AccessPassCreatedDto> CreateAsync(AccessPassCreateDto dto, Guid createdByUserId, CancellationToken ct = default);
    Task RevokeAsync(Guid id, Guid revokedByUserId, CancellationToken ct = default);
}

public class AccessPassService : IAccessPassService
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ISecureTokenService _tokens;
    private readonly IQrImageGenerator _qrImages;

    public AccessPassService(IAppDbContext db, ITenantContext tenant, ISecureTokenService tokens, IQrImageGenerator qrImages)
    {
        _db = db;
        _tenant = tenant;
        _tokens = tokens;
        _qrImages = qrImages;
    }

    public async Task<List<AccessPassDto>> GetAllAsync(Guid? houseId, CancellationToken ct = default)
    {
        var query = _db.AccessPasses.Include(p => p.House).AsQueryable();
        if (houseId.HasValue) query = query.Where(p => p.HouseId == houseId.Value);
        if (_tenant.EsResidente)
        {
            var misCasas = _db.Residents.Where(r => r.UserId == _tenant.UserId).Select(r => r.HouseId);
            query = query.Where(p => misCasas.Contains(p.HouseId));
        }

        var passes = await query.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
        var ahora = DateTime.UtcNow;

        // La generación de la imagen QR no se puede traducir a SQL, así que se hace en memoria
        // tras materializar la lista. Solo se regenera si el pase sigue realmente vigente:
        // ni el Status en base ni el tiempo transcurrido deben dejarlo pasar por separado.
        return passes.Select(p => new AccessPassDto(
            p.Id, p.HouseId, p.House!.Number, p.VisitorName, p.VehiclePlate,
            p.ValidFrom, p.ValidUntil, p.MaxUses, p.CurrentUses, p.Status, p.CreatedAt, p.RevokedAt,
            p.Status == AccessPassStatus.Active && ahora <= p.ValidUntil && !string.IsNullOrEmpty(p.QrContent)
                ? _qrImages.GeneratePngBase64(p.QrContent!)
                : null
        )).ToList();
    }

    public async Task<AccessPassCreatedDto> CreateAsync(AccessPassCreateDto dto, Guid createdByUserId, CancellationToken ct = default)
    {
        var house = await _db.Houses.FirstOrDefaultAsync(h => h.Id == dto.HouseId, ct)
            ?? throw new NotFoundException(nameof(House), dto.HouseId);

        var (selectorId, verifier, verifierHash) = _tokens.GenerateNewToken();
        var qrContent = $"{selectorId}.{verifier}";

        // Política fija para QRs de visita generados por un residente: siempre un solo uso,
        // siempre vigente desde ahora y hasta 24 horas después ("de hoy a mañana"). Nunca se
        // confía en lo que mande el cliente para esto - de ahí que ni siquiera esté en el DTO.
        var ahora = DateTime.UtcNow;
        var pass = new AccessPass
        {
            TenantId = house.TenantId,
            HouseId = dto.HouseId,
            CreatedByUserId = createdByUserId,
            VisitorName = dto.VisitorName,
            VehiclePlate = dto.VehiclePlate,
            Notes = dto.Notes,
            SelectorId = selectorId,
            VerifierHash = verifierHash,
            QrContent = qrContent,
            ValidFrom = ahora,
            ValidUntil = ahora.AddHours(24),
            MaxUses = 1
        };
        _db.AccessPasses.Add(pass);
        await _db.SaveChangesAsync(ct);

        var qrImageBase64 = _qrImages.GeneratePngBase64(qrContent);
        return new AccessPassCreatedDto(pass.Id, pass.VisitorName, pass.ValidFrom, pass.ValidUntil, pass.MaxUses, qrContent, qrImageBase64);
    }

    public async Task RevokeAsync(Guid id, Guid revokedByUserId, CancellationToken ct = default)
    {
        var pass = await _db.AccessPasses.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccessPass), id);

        if (pass.Status is AccessPassStatus.Used or AccessPassStatus.Revoked)
            throw new ValidationAppException($"El pase ya está en estado {pass.Status} y no se puede revocar.");

        pass.Status = AccessPassStatus.Revoked;
        pass.RevokedAt = DateTime.UtcNow;
        pass.RevokedByUserId = revokedByUserId;
        await _db.SaveChangesAsync(ct);
    }
}
