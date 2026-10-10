using AccessControl.Domain.Enums;

namespace AccessControl.Application.DTOs;

public record InventoryItemDto(
    Guid Id, Guid TenantId, string TenantName, InventoryCategory Category, string Name,
    string? Brand, string? Model, string? SerialNumber, InventoryStatus Status, string? Location,
    DateTime? InstallationDate, DateTime? PurchaseDate, DateTime? WarrantyUntil, string? Provider,
    decimal? Cost, string? Notes, Guid? LinkedDeviceId, string? LinkedDeviceName);

public record InventoryItemCreateDto(
    // Solo lo usa SUPER_ADMIN para elegir a qué privada pertenece; ADMIN_PRIVADA siempre
    // crea en su propio tenant sin importar lo que envíe aquí.
    Guid? TenantId,
    InventoryCategory Category, string Name, string? Brand, string? Model, string? SerialNumber,
    string? Location, DateTime? InstallationDate, DateTime? PurchaseDate, DateTime? WarrantyUntil,
    string? Provider, decimal? Cost, string? Notes, Guid? LinkedDeviceId);

public record InventoryItemUpdateDto(
    InventoryCategory Category, string Name, string? Brand, string? Model, string? SerialNumber,
    InventoryStatus Status, string? Location, DateTime? InstallationDate, DateTime? PurchaseDate,
    DateTime? WarrantyUntil, string? Provider, decimal? Cost, string? Notes, Guid? LinkedDeviceId);
