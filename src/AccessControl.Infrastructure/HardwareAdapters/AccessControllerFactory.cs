using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;

namespace AccessControl.Infrastructure.HardwareAdapters;

// Único punto donde se decide qué implementación de IAccessController usar.
// Agregar Hikvision/Shelly reales el día de mañana es sumar un case aquí + una clase nueva,
// sin tocar Application ni los controllers de la Api.
public class AccessControllerFactory : IAccessControllerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public AccessControllerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IAccessController Resolve(Device device)
    {
        return device.DriverType switch
        {
            DriverType.Mock => (IAccessController)_serviceProvider.GetService(typeof(MockAccessController))!,
            DriverType.Hikvision => (IAccessController)_serviceProvider.GetService(typeof(HikvisionAccessController))!,
            DriverType.HikvisionViaAgente => (IAccessController)_serviceProvider.GetService(typeof(RemoteAgentAccessController))!,
            // DriverType.ShellyCloud => futuro ShellyCloudAccessController,
            // DriverType.GenericRelay => futuro GenericRelayAccessController,
            _ => (IAccessController)_serviceProvider.GetService(typeof(MockAccessController))!
        };
    }
}
