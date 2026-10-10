using AccessControl.Domain.Enums;

namespace AccessControl.Application.DTOs;

// Sin ValidFrom/ValidUntil/MaxUses: para el QR de visita de un residente, esas reglas
// las decide el backend (siempre 1 solo uso, siempre vigente de ahora a +24 horas),
// nunca el cliente. Ver AccessPassService.CreateAsync.
public record AccessPassCreateDto(Guid HouseId, string VisitorName, string? VehiclePlate, string? Notes);

// Solo se devuelve completo (con el QrContent) en el momento de la creación.
public record AccessPassCreatedDto(
    Guid Id, string VisitorName, DateTime ValidFrom, DateTime ValidUntil, int MaxUses,
    string QrContent, string QrImageBase64);

public record AccessPassDto(
    Guid Id, Guid HouseId, string HouseNumber, string VisitorName, string? VehiclePlate,
    DateTime ValidFrom, DateTime ValidUntil, int MaxUses, int CurrentUses,
    AccessPassStatus Status, DateTime CreatedAt, DateTime? RevokedAt,
    // Solo viene poblado si el pase sigue Activo y dentro de su ventana de vigencia.
    string? QrImageBase64);
