using System.Net;
using System.Text;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.HardwareAdapters;

// Driver real para paneles Hikvision de la serie DS-K2800 (confirmado en sitio: DS-K2814,
// 4 puertas / 8 lectores) usando ISAPI, la API HTTP abierta de Hikvision. No requiere el
// SDK nativo (HCNetSDK): el panel expone su propio servidor web en la LAN con estos endpoints.
//
// Se usa cuando el backend SÍ puede alcanzar directamente la IP del panel (misma LAN).
// Cuando el backend está centralizado y no tiene esa ruta de red, se usa en su lugar
// RemoteAgentAccessController, que manda este mismo tipo de comando a través de un Agente
// local vía SignalR — ese Agente reutiliza esta misma lógica de llamada ISAPI.
//
// Autenticación: HTTP Digest con el usuario/contraseña de administrador configurados en el
// panel. .NET soporta Digest de forma nativa a través de HttpClientHandler.Credentials.
//
// Endpoint de apertura remota (uno de los 4 números de puerta del panel, mapeado 1:1 al
// ChannelNumber de DeviceAccessPoint):
//   PUT /ISAPI/AccessControl/RemoteControl/door/{doorNo}
//   body: <RemoteControlDoor><cmd>open</cmd></RemoteControlDoor>
public class HikvisionAccessController : IAccessController
{
    private readonly ILogger<HikvisionAccessController> _logger;

    public HikvisionAccessController(ILogger<HikvisionAccessController> logger)
    {
        _logger = logger;
    }

    public async Task<ConnectionStatusResult> ValidateConnectionAsync(Device device, CancellationToken ct = default)
    {
        try
        {
            var config = HikvisionConfig.Leer(device);
            using var cliente = CrearCliente(config);
            var url = $"{UrlBase(device, config)}/ISAPI/System/deviceInfo";

            var respuesta = await cliente.GetAsync(url, ct);
            var cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
            return new ConnectionStatusResult(respuesta.IsSuccessStatusCode, cuerpo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo validar conexión con panel Hikvision {DeviceId}", device.Id);
            return new ConnectionStatusResult(false, ex.Message);
        }
    }

    public async Task<OpenResult> OpenAsync(Device device, OpenRequest request, CancellationToken ct = default)
    {
        try
        {
            var config = HikvisionConfig.Leer(device);
            using var cliente = CrearCliente(config);
            var url = $"{UrlBase(device, config)}/ISAPI/AccessControl/RemoteControl/door/{request.ChannelNumber}";

            var cuerpoXml = "<RemoteControlDoor><cmd>open</cmd></RemoteControlDoor>";
            var contenido = new StringContent(cuerpoXml, Encoding.UTF8, "application/xml");

            var respuesta = await cliente.PutAsync(url, contenido, ct);
            var cuerpoRespuesta = await respuesta.Content.ReadAsStringAsync(ct);

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Hikvision respondió {Status} al abrir puerta {Puerta} en {DeviceId}: {Cuerpo}",
                    respuesta.StatusCode, request.ChannelNumber, device.Id, cuerpoRespuesta);
            }

            return new OpenResult(
                respuesta.IsSuccessStatusCode, cuerpoRespuesta,
                respuesta.IsSuccessStatusCode ? null : $"HTTP {(int)respuesta.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abriendo puerta {Puerta} en panel Hikvision {DeviceId}", request.ChannelNumber, device.Id);
            return new OpenResult(false, null, ex.Message);
        }
    }

    public async Task<DeviceStatusResult> GetStatusAsync(Device device, CancellationToken ct = default)
    {
        var estado = await ValidateConnectionAsync(device, ct);
        return new DeviceStatusResult(estado.IsReachable, estado.Details);
    }

    internal static HttpClient CrearCliente(HikvisionConfig config)
    {
        var handler = new HttpClientHandler
        {
            Credentials = new NetworkCredential(config.Username, config.Password),
            // Los paneles Hikvision suelen usar un certificado autofirmado en HTTPS.
            ServerCertificateCustomValidationCallback = config.UseHttps
                ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                : null
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(8) };
    }

    internal static string UrlBase(Device device, HikvisionConfig config)
    {
        if (string.IsNullOrWhiteSpace(device.IpAddress))
            throw new InvalidOperationException($"El dispositivo '{device.Name}' no tiene una IP configurada.");

        var esquema = config.UseHttps ? "https" : "http";
        return $"{esquema}://{device.IpAddress}:{config.Port}";
    }
}
