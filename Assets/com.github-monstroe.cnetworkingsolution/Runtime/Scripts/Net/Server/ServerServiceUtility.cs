using System;
using System.Collections.Generic;

public class ServerServiceUtility
{
    private Dictionary<ulong, ServerService> services = new Dictionary<ulong, ServerService>();
    private Dictionary<Type, ulong> serviceTypeCache = new Dictionary<Type, ulong>();
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
        ulong serviceId = service.ServiceId;
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
            return true;
        }
        return false;
    }

    public bool UnregisterService(ulong serviceId)
    {
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

    public T GetService<T>(out ulong serviceId) where T : ServerService
    {
        if (serviceTypeCache.TryGetValue(typeof(T), out serviceId) && services.TryGetValue(serviceId, out ServerService service))
        {
            return (T)service;
        }
        return null;
    }

    public bool GetService(ulong serviceId, out ServerService service)
    {
        if (services.TryGetValue(serviceId, out service))
        {
            return true;
        }
        return false;
    }
}
