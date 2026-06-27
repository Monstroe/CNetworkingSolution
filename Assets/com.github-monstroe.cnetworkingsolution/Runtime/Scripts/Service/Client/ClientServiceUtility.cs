using System;
using System.Collections.Generic;

namespace Monstroe.CNetworkingSolution
{
    public class ClientServiceUtility
    {
        private readonly Dictionary<ulong, ClientService> services = new Dictionary<ulong, ClientService>();
        private readonly ServiceBus serviceBus = new ServiceBus();

        public bool RegisterService<T>(T service, out ulong serviceId) where T : ClientService
        {
            if (serviceBus.RegisterService(service.GetType(), out serviceId) && !services.ContainsKey(serviceId))
            {
                services[serviceId] = service;
                return true;
            }
            return false;
        }

        public bool UnregisterService(ulong serviceId)
        {
            if (services.TryGetValue(serviceId, out ClientService service) && serviceBus.UnregisterService(service.GetType()))
            {
                services.Remove(serviceId);
                return true;
            }
            return false;
        }

        public T GetService<T>(out ulong serviceId) where T : ClientService
        {
            if (serviceBus.TryGetServiceId<T>(out serviceId) && services.TryGetValue(serviceId, out ClientService service))
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

        public bool TryGetServiceId<T>(out ulong serviceId) where T : ClientService
        {
            return serviceBus.TryGetServiceId<T>(out serviceId);
        }
    }
}