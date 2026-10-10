using Microsoft.AspNetCore.Identity;

namespace AccessControl.Api.Controllers;

internal static class MensajesContrasena
{
    public static string Traducir(IEnumerable<IdentityError> errores)
    {
        var mensajes = errores.Select(e => e.Code switch
        {
            "PasswordTooShort" => "La contraseña debe tener al menos 10 caracteres.",
            "PasswordRequiresUpper" => "La contraseña debe incluir al menos una mayúscula.",
            "PasswordRequiresLower" => "La contraseña debe incluir al menos una minúscula.",
            "PasswordRequiresDigit" => "La contraseña debe incluir al menos un número.",
            "PasswordMismatch" => "La contraseña actual no es correcta.",
            "DuplicateEmail" or "DuplicateUserName" => "Ya existe una cuenta con ese correo.",
            _ => e.Description
        });
        return string.Join(" ", mensajes);
    }
}
