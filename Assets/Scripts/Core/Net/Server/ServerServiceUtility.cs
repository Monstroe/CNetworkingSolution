using System;
using System.Collections.Generic;

public class ServerServiceUtility
{
    private Dictionary<ServiceType, ServerService> services = new Dictionary<ServiceType, ServerService>();
    private Dictionary<Type, ServiceType> serviceTypeCache = new Dictionary<Type, ServiceType>();
    private SortedDictionary<int, List<ServerService>> serviceOrderCache = new SortedDictionary<int, List<ServerService>>();

    public void UserJoined(UserData user)
    {
        foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
        {
            foreach (ServerService service in serviceList)
            {
                service.UserJoined(user);
            }
        }
    }

    public void UserJoinedGame(UserData user)
    {
        foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
        {
            foreach (ServerService service in serviceList)
            {
                service.UserJoinedGame(user);
            }
        }
    }

    public void UserLeft(UserData user)
    {
        foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
        {
            foreach (ServerService service in serviceList)
            {
                service.UserLeft(user);
            }
        }
    }

    public void Tick()
    {
        foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
        {
            foreach (ServerService service in serviceList)
            {
                service.Tick();
            }
        }
    }

    public bool RegisterService<T>(T service) where T : ServerService
    {
        ServiceType serviceType = service.ServiceType;
        int executionOrder = service.ExecutionOrder;
        if (!services.ContainsKey(serviceType))
        {
            services[serviceType] = service;
            serviceTypeCache[service.GetType()] = serviceType;

            if (!serviceOrderCache.TryGetValue(executionOrder, out List<ServerService> list))
            {
                list = new List<ServerService>();
                serviceOrderCache[executionOrder] = list;
            }
            list.Add(service);
            return true;
        }
        return false;
    }

    public bool UnregisterService<T>()
    {
        ServiceType serviceType = serviceTypeCache[typeof(T)];
        if (services.ContainsKey(serviceType))
        {
            services.Remove(serviceType);
            return true;
        }
        return false;
    }

    public T GetService<T>() where T : ServerService
    {
        if (serviceTypeCache.TryGetValue(typeof(T), out ServiceType serviceType) && services.TryGetValue(serviceType, out ServerService service))
        {
            return (T)service;
        }
        return null;
    }

    public bool GetService(ServiceType serviceType, out ServerService service)
    {
        if (services.TryGetValue(serviceType, out service))
        {
            return true;
        }
        return false;
    }
}
