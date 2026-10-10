namespace AccessControl.Domain.Agents;

// Contrato compartido entre el backend (que lo manda por el Hub de SignalR) y el
// programa Agente (que lo recibe y ejecuta la llamada ISAPI real). Vive en Domain,
// sin dependencias externas, para que el proyecto del Agente pueda referenciarlo
// sin arrastrar EF Core / ASP.NET Identity que trae Infrastructure.
public record AbrirPuertaComando(
    Guid DeviceId,
    string IpAddress,
    int Port,
    bool UseHttps,
    string Username,
    string Password,
    int ChannelNumber);
