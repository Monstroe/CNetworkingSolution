using System;
using System.Collections.Generic;

public class ServerServiceUtility : ServiceUtility
{
    private Dictionary<uint, ServerService> services = new Dictionary<uint, ServerService>();
    private Dictionary<Type, uint> serviceTypeCache = new Dictionary<Type, uint>();
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

    public uint? RegisterService<T>(T service) where T : ServerService
    {
        uint serviceId = GenerateServiceId(service.GetType());
        int executionOrder = service.ExecutionOrder;
        if (!services.ContainsKey(serviceId))
        {
            services[serviceId] = service;
            serviceTypeCache[service.GetType()] = serviceId;

            if (!serviceOrderCache.TryGetValue(executionOrder, out List<ServerService> list))
            {
                list = new List<ServerService>();
                serviceOrderCache[executionOrder] = list;
            }
            list.Add(service);
            return serviceId;
        }
        return null;
    }

    public bool UnregisterService<T>(out uint serviceId) where T : ServerService
    {
        serviceId = serviceTypeCache[typeof(T)];
        if (services.TryGetValue(serviceId, out ServerService service))
        {
            services.Remove(serviceId);
            serviceTypeCache.Remove(service.GetType());
            List<ServerService> executionList = serviceOrderCache[service.ExecutionOrder];
            executionList.Remove(service);
            if (executionList.Count == 0)
            {
                serviceOrderCache.Remove(service.ExecutionOrder);
            }
            return true;
        }
        return false;
    }

    public T GetService<T>(out uint serviceId) where T : ServerService
    {
        if (serviceTypeCache.TryGetValue(typeof(T), out serviceId) && services.TryGetValue(serviceId, out ServerService service))
        {
            return (T)service;
        }
        return null;
    }

    public bool GetService(uint serviceId, out ServerService service)
    {
        if (services.TryGetValue(serviceId, out service))
        {
            return true;
        }
        return false;
    }
}
