using System;
using System.Collections.Generic;
using UnityEngine;

public class ClientLobby : Lobby
{
    public ClientLobbyGameData GameData { get; private set; } = new ClientLobbyGameData();
    public Map Map { get; private set; }

    private Dictionary<ServiceType, ClientService> services = new Dictionary<ServiceType, ClientService>();

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
    private string gameServerToken;
#endif
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public override void Init(int lobbyId, NetTransport transport)
    {
        base.Init(lobbyId, transport);
        transport.Initialize();
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
            Debug.Log($"<color=red><b>CNS</b></color>: Unregistered ClientService {serviceType}.");
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
            Debug.LogWarning($"<color=red><b>CNS</b></color>: ClientService {serviceType} not found.");
            return null;
        }
    }
}
