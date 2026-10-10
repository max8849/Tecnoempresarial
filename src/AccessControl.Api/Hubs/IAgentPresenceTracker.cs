namespace AccessControl.Api.Hubs;

// Registro en memoria de qué privadas tienen su Agente conectado ahora mismo.
// Suficiente para una sola instancia del backend; si el día de mañana se corre en
// varias instancias en paralelo, esto tendría que moverse a algo compartido (ej. Redis).
public interface IAgentPresenceTracker
{
    void Conectado(Guid tenantId);
    void Desconectado(Guid tenantId);
    bool EstaConectado(Guid tenantId);
}
