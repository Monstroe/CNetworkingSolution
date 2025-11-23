using System;
using System.Collections.Generic;

public class ClientServiceUtility
{
    private Dictionary<ServiceType, ClientService> services = new Dictionary<ServiceType, ClientService>();
    private Dictionary<Type, ServiceType> serviceTypeCache = new Dictionary<Type, ServiceType>();

    public bool RegisterService<T>(T service) where T : ClientService
    {
        ServiceType serviceType = service.ServiceType;
        if (!services.ContainsKey(serviceType))
        {
            services[serviceType] = service;
            serviceTypeCache[service.GetType()] = serviceType;
            return true;
        }
        return false;
    }

    public bool UnregisterService<T>() where T : ClientService
    {
        ServiceType serviceType = serviceTypeCache[typeof(T)];
        if (services.ContainsKey(serviceType))
        {
            services.Remove(serviceType);
            return true;
        }
        return false;
    }

    public T GetService<T>() where T : ClientService
    {
        if (serviceTypeCache.TryGetValue(typeof(T), out ServiceType serviceType) && services.TryGetValue(serviceType, out ClientService service))
        {
            return (T)service;
        }
        return null;
    }

    public bool GetService(ServiceType serviceType, out ClientService service)
    {
        if (services.TryGetValue(serviceType, out service))
        {
            return true;
        }
        return false;
    }
}
