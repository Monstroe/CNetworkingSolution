using System;
using System.Collections.Generic;

namespace CNetworkingSolution
{
    public class ServerServiceUtility
    {
        private readonly Dictionary<ulong, ServerService> services = new Dictionary<ulong, ServerService>();
        private readonly SortedDictionary<int, List<ServerService>> serviceOrderCache = new SortedDictionary<int, List<ServerService>>();
        private readonly ServiceBus serviceBus = new ServiceBus();

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

        public void LateUserJoined(UserData user)
        {
            foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
            {
                foreach (ServerService service in serviceList)
                {
                    service.LateUserJoined(user);
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

        public void LateUserJoinedGame(UserData user)
        {
            foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
            {
                foreach (ServerService service in serviceList)
                {
                    service.LateUserJoinedGame(user);
                }
            }
        }

        public void UserLeftGame(UserData user)
        {
            foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
            {
                foreach (ServerService service in serviceList)
                {
                    service.UserLeftGame(user);
                }
            }
        }

        public void LateUserLeftGame(UserData user)
        {
            foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
            {
                foreach (ServerService service in serviceList)
                {
                    service.LateUserLeftGame(user);
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

        public void LateUserLeft(UserData user)
        {
            foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
            {
                foreach (ServerService service in serviceList)
                {
                    service.LateUserLeft(user);
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

        public bool RegisterService<T>(T service, out ulong serviceId) where T : ServerService
        {
            int executionOrder = service.ExecutionOrder;
            if (serviceBus.RegisterService(service.GetType(), out serviceId) && !services.ContainsKey(serviceId))
            {
                services[serviceId] = service;
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
            if (services.TryGetValue(serviceId, out ServerService service) && serviceBus.UnregisterService(service.GetType()))
            {
                services.Remove(serviceId);
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
            if (serviceBus.TryGetServiceId<T>(out serviceId) && services.TryGetValue(serviceId, out ServerService service))
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

        public bool TryGetServiceId<T>(out ulong serviceId) where T : ServerService
        {
            return serviceBus.TryGetServiceId<T>(out serviceId);
        }
    }
}