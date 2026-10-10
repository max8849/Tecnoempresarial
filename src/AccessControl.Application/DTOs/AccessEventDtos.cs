using AccessControl.Domain.Enums;

namespace AccessControl.Application.DTOs;

public record AccessEventDto(
    Guid Id, string? AccessPointName, string? DeviceName, string? VisitorName,
    AccessEventType EventType, AccessEventResult Result, DateTime Timestamp, string? Metadata);
