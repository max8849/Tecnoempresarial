namespace AccessControl.Application.DTOs;

public record LoginRequestDto(string Email, string Password);

public record LoginResponseDto(
    string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken,
    string Email, string FullName, string Role, Guid? TenantId);

public record RefreshTokenRequestDto(string RefreshToken);

public record CambiarContrasenaDto(string ContrasenaActual, string ContrasenaNueva);
