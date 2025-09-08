using System;
using System.Collections.Generic;
using UnityEngine;

public class ClientLobby : MonoBehaviour
{
    public LobbyData LobbyData { get; protected set; } = new LobbyData();
    public ClientLobbyGameData GameData { get; private set; } = new ClientLobbyGameData();

    private NetTransport transport;
    private Dictionary<ServiceType, ClientService> services = new Dictionary<ServiceType, ClientService>();

    public void Init(int lobbyId, NetTransport transport)
    {
        LobbyData.LobbyId = lobbyId;
        this.transport = transport;
        transport.Initialize(NetDeviceType.Client);
    }

    public void SendToServer(NetPacket packet, TransportMethod method)
    {
        transport.SendToAll(packet, method);
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

    public void RegisterService(ServiceType serviceType, ClientService service)
    {
        if (!services.ContainsKey(serviceType))
        {
            services[serviceType] = service;
            Debug.Log($"<color=green><b>CNS</b></color>: Registered ClientService {serviceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {serviceType} is already registered.");
        }
    }

    public void UnregisterService(ServiceType serviceType)
    {
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

    public ClientService GetService(ServiceType serviceType)
    {
        if (services.TryGetValue(serviceType, out ClientService service))
        {
            return service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {serviceType} not found.");
            return null;
        }
    }
}
