namespace AccessControl.Application.DTOs;

public record TenantDto(Guid Id, string Name, string Slug, bool IsActive, int TotalHouses);

public record TenantCreateDto(string Name, string Slug);

public record TenantUpdateDto(string Name, bool IsActive);
