using System;
using System.Collections.Generic;

public class ClientServiceUtility : ServiceUtility
{
    private Dictionary<uint, ClientService> services = new Dictionary<uint, ClientService>();
    private Dictionary<Type, uint> serviceTypeCache = new Dictionary<Type, uint>();

    public uint? RegisterService<T>(T service) where T : ClientService
    {
        uint serviceId = GenerateServiceId(service.GetType());
        if (!services.ContainsKey(serviceId))
        {
            services[serviceId] = service;
            serviceTypeCache[service.GetType()] = serviceId;
            return serviceId;
        }
        return null;
    }

    public bool UnregisterService<T>(out uint serviceId) where T : ClientService
    {
        serviceId = serviceTypeCache[typeof(T)];
        if (services.TryGetValue(serviceId, out ClientService service))
        {
            services.Remove(serviceId);
            serviceTypeCache.Remove(service.GetType());
            return true;
        }
        return false;
    }

    public T GetService<T>(out uint serviceId) where T : ClientService
    {
        if (serviceTypeCache.TryGetValue(typeof(T), out serviceId) && services.TryGetValue(serviceId, out ClientService service))
        {
            return (T)service;
        }
        return null;
    }

    public bool GetService(uint serviceId, out ClientService service)
    {
        if (services.TryGetValue(serviceId, out service))
        {
            return true;
        }
        return false;
    }
}
