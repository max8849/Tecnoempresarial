using System.Text.Json;
using AccessControl.Domain.Entities;

namespace AccessControl.Infrastructure.HardwareAdapters;

// Se guarda en Device.ConfigurationJson. Nota de seguridad pendiente: la contraseña queda
// en texto plano en la base de datos, igual que se decidió para AccessPass.QrContent.
// Antes de producción real, cifrar con la Data Protection API de ASP.NET Core.
public class HikvisionConfig
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public bool UseHttps { get; set; }

    public static HikvisionConfig Leer(Device device)
    {
        if (string.IsNullOrWhiteSpace(device.ConfigurationJson))
            throw new InvalidOperationException(
                $"El dispositivo '{device.Name}' no tiene configurado usuario/contraseña de Hikvision (ConfigurationJson vacío).");

        var config = JsonSerializer.Deserialize<HikvisionConfig>(device.ConfigurationJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (config is null || string.IsNullOrWhiteSpace(config.Username) || string.IsNullOrWhiteSpace(config.Password))
            throw new InvalidOperationException($"Configuración de Hikvision incompleta para el dispositivo '{device.Name}'.");

        return config;
    }
}
