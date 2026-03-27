using System;
using System.Collections.Generic;

public class ClientServiceUtility
{
    private Dictionary<ulong, ClientService> services = new Dictionary<ulong, ClientService>();
    private Dictionary<Type, ulong> serviceTypeCache = new Dictionary<Type, ulong>();

    public bool RegisterService<T>(T service) where T : ClientService
    {
        ulong serviceId = service.ServiceId;
        if (!services.ContainsKey(serviceId))
        {
            services[serviceId] = service;
            serviceTypeCache[service.GetType()] = serviceId;
            return true;
        }
        return false;
    }

    public bool UnregisterService<T>(out ulong serviceId) where T : ClientService
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

    public T GetService<T>(out ulong serviceId) where T : ClientService
    {
        if (serviceTypeCache.TryGetValue(typeof(T), out serviceId) && services.TryGetValue(serviceId, out ClientService service))
        {
            return (T)service;
        }
        return null;
    }

    public bool GetService(ulong serviceId, out ClientService service)
    {
        if (services.TryGetValue(serviceId, out service))
        {
            return true;
        }
        return false;
    }
}
