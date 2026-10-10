namespace AccessControl.Domain.Enums;

public enum AccessPassStatus
{
    Active,
    Used,
    Expired,
    Revoked,
    Disabled
}

public enum AccessPointType
{
    VehicularGate,
    PedestrianGate,
    MainEntrance,
    ServiceEntrance,
    Other
}

public enum AccessDirection
{
    In,
    Out,
    Both
}

public enum DeviceStatus
{
    Unknown,
    Online,
    Offline,
    Error
}

// Identifica qué implementación de IAccessController debe usarse para este dispositivo.
// Hoy solo existe Mock; el resto se activan cuando se conozca/integre el hardware real.
public enum DriverType
{
    Mock,
    ShellyCloud,
    // Hikvision alcanzable directo desde el backend (misma LAN que el panel).
    Hikvision,
    // Hikvision alcanzable solo a través del Agente local de la privada (backend
    // centralizado sin acceso directo a esa LAN): el comando viaja por SignalR.
    HikvisionViaAgente,
    GenericRelay
}

public enum AccessEventType
{
    QrScanned,
    AccessAuthorized,
    AccessDenied,
    OpenCommandSent,
    OpenCommandFailed,
    ManualOpen,
    PassRevoked,
    PassExpired
}

public enum AccessEventResult
{
    Authorized,
    CommandSent,
    Opened,
    Failed,
    Denied
}

public enum HouseStatus
{
    Active,
    Inactive
}

public enum ResidentStatus
{
    Active,
    Inactive
}

public enum InventoryCategory
{
    GateMotor,
    Barrier,
    HikvisionPanel,
    Camera,
    Reader,
    Relay,
    NetworkEquipment,
    Ups,
    Cabling,
    Other
}

public enum InventoryStatus
{
    Active,
    InRepair,
    InStorage,
    Decommissioned
}
