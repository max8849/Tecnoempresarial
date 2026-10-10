using AccessControl.Domain.Enums;

namespace AccessControl.Application.DTOs;

public record ResidentDto(
    Guid Id, Guid HouseId, string HouseNumber, string FirstName, string LastName,
    string? Phone, string? Email, ResidentStatus Status, DateTime StartDate, DateTime? EndDate, Guid TenantId, string TenantName);

public record ResidentCreateDto(Guid HouseId, string FirstName, string LastName, string? Phone, string? Email);

public record ResidentUpdateDto(string FirstName, string LastName, string? Phone, string? Email, ResidentStatus Status, DateTime? EndDate);
