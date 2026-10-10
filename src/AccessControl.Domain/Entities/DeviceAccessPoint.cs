namespace AccessControl.Domain.Entities;

// Tabla puente: qué canal/relay de qué Device controla qué AccessPoint.
public class DeviceAccessPoint
{
    public Guid DeviceId { get; set; }
    public Device? Device { get; set; }

    public Guid AccessPointId { get; set; }
    public AccessPoint? AccessPoint { get; set; }

    // Número de canal/relay dentro del dispositivo (paneles multi-puerta suelen tener varios).
    public int ChannelNumber { get; set; } = 1;
}
