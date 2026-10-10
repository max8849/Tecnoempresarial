using AccessControl.Domain.Enums;

namespace AccessControl.Application.DTOs;

public record HouseDto(Guid Id, string Number, string? Notes, HouseStatus Status, int TotalResidents, Guid TenantId, string TenantName);

public record HouseCreateDto(string Number, string? Notes, Guid? TenantId = null);

public record HouseUpdateDto(string Number, string? Notes, HouseStatus Status);
