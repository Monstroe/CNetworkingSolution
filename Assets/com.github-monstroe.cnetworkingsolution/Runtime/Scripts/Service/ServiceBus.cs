using System;
using System.Collections.Generic;
using System.Reflection;

public class ServiceBus
{
    private readonly Dictionary<Type, ulong> serviceTypeCache = new Dictionary<Type, ulong>();

    public bool RegisterService(Type serviceType, out ulong serviceId)
    {
        if (serviceTypeCache.TryGetValue(serviceType, out ulong id))
        {
            serviceId = id;
            return false;
        }

        id = GetServiceId(serviceType);
        serviceTypeCache[serviceType] = id;
        serviceId = id;
        return true;
    }

    public bool UnregisterService(Type serviceType)
    {
        return serviceTypeCache.Remove(serviceType);
    }

    public bool TryGetServiceId(Type type, out ulong serviceId)
    {
        return serviceTypeCache.TryGetValue(type, out serviceId);
    }

    public bool TryGetServiceId<T>(out ulong serviceId)
    {
        return TryGetServiceId(typeof(T), out serviceId);
    }

    public static ulong GetServiceId(Type serviceType)
    {
        var attr = serviceType.GetCustomAttribute<ServiceIdAttribute>() ?? throw new Exception($"ServiceIdAttribute missing on {serviceType.FullName}");
        return NetResources.GenerateHashKey(attr.ServiceId);
    }
}