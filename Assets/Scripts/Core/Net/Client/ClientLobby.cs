using System;
using System.Collections.Generic;
using UnityEngine;

public class ClientLobby : MonoBehaviour
{
    public LobbyData LobbyData { get; private set; } = new LobbyData();

    private NetTransport transport;
    private Dictionary<ServiceType, ClientService> services = new Dictionary<ServiceType, ClientService>();
    private Dictionary<Type, ServiceType> serviceTypeCache = new Dictionary<Type, ServiceType>();

    public void Init(int lobbyId, NetTransport transport)
    {
        LobbyData.LobbyId = lobbyId;
        this.transport = transport;
    }

    public void SendToServer(NetPacket packet, TransportMethod method)
    {
        if (packet != null)
        {
            transport.SendToAll(packet, method);
        }
    }

    public void ReceiveData(NetPacket packet, TransportMethod? transportMethod)
    {
        ServiceType serviceType = (ServiceType)packet.ReadByte();
        CommandType commandType = (CommandType)packet.ReadByte();

        if (services.TryGetValue(serviceType, out ClientService service))
        {
            service.ReceiveData(packet, serviceType, commandType, transportMethod);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No service found for type {serviceType}. Command {commandType} will not be processed.");
        }
    }

    public void RegisterService<T>(T service) where T : ClientService
    {
        ServiceType serviceType = service.ServiceType;
        if (!services.ContainsKey(serviceType))
        {
            services[serviceType] = service;
            serviceTypeCache[service.GetType()] = serviceType;

            Debug.Log($"<color=green><b>CNS</b></color>: Registered ClientService {serviceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {serviceType} is already registered.");
        }
    }

    public void UnregisterService<T>()
    {
        ServiceType serviceType = serviceTypeCache[typeof(T)];
        if (services.ContainsKey(serviceType))
        {
            services.Remove(serviceType);
            Debug.Log($"<color=green><b>CNS</b></color>: Unregistered ClientService {serviceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {serviceType} is not registered.");
        }
    }

    public T GetService<T>() where T : ClientService
    {
        if (serviceTypeCache.TryGetValue(typeof(T), out ServiceType serviceType) && services.TryGetValue(serviceType, out ClientService service))
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {serviceType} not found.");
            return null;
        }
    }
}
