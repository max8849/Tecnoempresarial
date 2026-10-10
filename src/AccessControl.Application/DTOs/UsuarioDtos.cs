namespace AccessControl.Application.DTOs;

public record UsuarioDto(
    Guid Id, string Email, string FullName, string Role, Guid? TenantId, string TenantName,
    string? Casa, Guid? ResidentId, bool Activo);

public record CrearUsuarioDto(
    string Email, string FullName, string Password, string Role, Guid? TenantId, Guid? ResidentId);

public record ActualizarUsuarioDto(string FullName);

public record RestablecerContrasenaDto(string NuevaContrasena);

public record ResidenteSinCuentaDto(Guid Id, string NombreCompleto, string Casa, Guid TenantId, string TenantName);
