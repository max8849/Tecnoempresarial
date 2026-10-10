using AccessControl.Domain.Enums;

namespace AccessControl.Application.DTOs;

public record AccessPointDto(Guid Id, string Name, AccessPointType Type, AccessDirection Direction, bool IsActive, Guid TenantId, string TenantName);

public record AccessPointCreateDto(string Name, AccessPointType Type, AccessDirection Direction, Guid? TenantId = null);
