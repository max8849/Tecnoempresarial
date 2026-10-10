namespace AccessControl.Application.DTOs;

public record ValidateTokenRequestDto(string Token);

// Respuesta de solo lectura/preview: NO consume usos ni dispara ninguna apertura.
public record ValidateTokenResponseDto(bool IsValid, string? DenialReason, string? VisitorName, string? HouseNumber, string? VehiclePlate);

public record ScanRequestDto(string Token, Guid? AccessPointId);

// Respuesta autoritativa: si IsAuthorized es true, ya se intentó abrir físicamente.
public record ScanResponseDto(bool IsAuthorized, string? DenialReason, bool OpenCommandSent, string? VisitorName, string? HouseNumber, string? VehiclePlate);
