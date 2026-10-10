using System.Net;
using System.Net.Http.Json;
using System.Text;
using AccessControl.Domain.Agents;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

// Programa Agente: corre de forma permanente en una mini PC dentro de la red de UNA
// privada. Se conecta hacia afuera al backend central (nunca al revés), se queda
// escuchando, y cuando recibe la orden "AbrirCanal" le habla directo al panel Hikvision
// por su IP local. No tiene base de datos, no valida QRs, no sabe nada de residentes:
// solo es un puente entre el backend central y el hardware de esta privada.

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables("AGENTE_")
    .Build();

var backendUrl = (config["BackendBaseUrl"] ?? throw new InvalidOperationException("Falta BackendBaseUrl en appsettings.json")).TrimEnd('/');
var agentApiKey = config["AgentApiKey"] ?? throw new InvalidOperationException("Falta AgentApiKey en appsettings.json (cópiala desde el panel de Super Administrador, sección Privadas).");

Console.WriteLine($"[Agente] Iniciando. Backend central: {backendUrl}");

var httpCliente = new HttpClient { BaseAddress = new Uri(backendUrl) };
string? tokenActual = null;
var tokenExpiraEn = DateTime.MinValue;

async Task<string?> ObtenerTokenAsync()
{
    if (tokenActual is not null && DateTime.UtcNow < tokenExpiraEn.AddMinutes(-1))
        return tokenActual;

    var respuesta = await httpCliente.PostAsJsonAsync("/api/agents/token", new { agentApiKey });
    respuesta.EnsureSuccessStatusCode();

    var datos = await respuesta.Content.ReadFromJsonAsync<TokenRespuesta>()
        ?? throw new InvalidOperationException("El backend respondió vacío al pedir el token.");

    tokenActual = datos.AccessToken;
    tokenExpiraEn = datos.ExpiresAt;
    Console.WriteLine($"[Agente] Token renovado para privada '{datos.TenantName}' (expira {tokenExpiraEn:u}).");
    return tokenActual;
}

async Task AbrirPuertaAsync(AbrirPuertaComando comando)
{
    var handler = new HttpClientHandler
    {
        Credentials = new NetworkCredential(comando.Username, comando.Password),
        ServerCertificateCustomValidationCallback = comando.UseHttps
            ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            : null
    };
    using var cliente = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(8) };

    var esquema = comando.UseHttps ? "https" : "http";
    var url = $"{esquema}://{comando.IpAddress}:{comando.Port}/ISAPI/AccessControl/RemoteControl/door/{comando.ChannelNumber}";
    var cuerpo = new StringContent("<RemoteControlDoor><cmd>open</cmd></RemoteControlDoor>", Encoding.UTF8, "application/xml");

    var respuesta = await cliente.PutAsync(url, cuerpo);
    var textoRespuesta = await respuesta.Content.ReadAsStringAsync();
    respuesta.EnsureSuccessStatusCode();
    Console.WriteLine($"[Agente] Hikvision respondió {(int)respuesta.StatusCode}: {textoRespuesta}");
}

var conexion = new HubConnectionBuilder()
    .WithUrl($"{backendUrl}/hubs/agentes", opciones => opciones.AccessTokenProvider = ObtenerTokenAsync)
    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
    .Build();

conexion.On<AbrirPuertaComando>("AbrirCanal", async comando =>
{
    Console.WriteLine($"[Agente] Orden recibida: abrir canal {comando.ChannelNumber} en {comando.IpAddress}:{comando.Port}");
    try
    {
        await AbrirPuertaAsync(comando);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Agente] ERROR al hablar con el panel Hikvision: {ex.Message}");
    }
});

conexion.Reconnecting += _ => { Console.WriteLine("[Agente] Conexión perdida, reintentando..."); return Task.CompletedTask; };
conexion.Reconnected += _ => { Console.WriteLine("[Agente] Reconectado al backend central."); return Task.CompletedTask; };
conexion.Closed += async _ =>
{
    Console.WriteLine("[Agente] Conexión cerrada definitivamente. Reintentando en 5s...");
    await Task.Delay(5000);
    await ConectarConReintentosAsync();
};

async Task ConectarConReintentosAsync()
{
    while (true)
    {
        try
        {
            await conexion.StartAsync();
            Console.WriteLine("[Agente] Conectado al backend central. Esperando órdenes...");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Agente] No se pudo conectar ({ex.Message}). Reintentando en 5s...");
            await Task.Delay(5000);
        }
    }
}

await ConectarConReintentosAsync();

var salir = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; salir.TrySetResult(); };
AppDomain.CurrentDomain.ProcessExit += (_, _) => salir.TrySetResult();
await salir.Task;

await conexion.DisposeAsync();
Console.WriteLine("[Agente] Detenido.");

record TokenRespuesta(string AccessToken, DateTime ExpiresAt, string TenantName);
