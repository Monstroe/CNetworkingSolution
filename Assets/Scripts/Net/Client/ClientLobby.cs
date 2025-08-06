using System;
using System.Collections.Generic;
using UnityEngine;

public class ClientLobby : Lobby
{
    public static ClientLobby Instance { get; private set; }

    private Dictionary<ServiceType, ClientService> services = new Dictionary<ServiceType, ClientService>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Multiple instances of ClientLobby detected. Destroying duplicate instance.");
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LobbyManager.Instance.OnLobbyCreated += HandleLobbyCreated;
        LobbyManager.Instance.OnLobbyJoined += HandleLobbyJoined;
        NetworkManager.Instance.OnNetworkReceived += ReceiveData;
    }

#if CNS_TOKEN_VERIFIER
    private void HandleLobbyCreated(LobbyData lobbyData, GameServerData gameServerData, string gameServerToken)
#else
    private void HandleLobbyCreated(LobbyData lobbyData, GameServerData gameServerData)
#endif
    {
        LobbyData = lobbyData;
    }

#if CNS_TOKEN_VERIFIER
    private void HandleLobbyJoined(LobbyData lobbyData, GameServerData gameServerData, string gameServerToken)
#else
    private void HandleLobbyJoined(LobbyData lobbyData, GameServerData gameServerData)
#endif
    {
        LobbyData = lobbyData;
    }

    public void SendToRoom(NetPacket packet, TransportMethod method)
    {
        transport.SendToAll(packet, method);
    }

    public void ReceiveData(ReceivedArgs args)
    {
        NetPacket packet = args.Packet;
        TransportMethod? transportMethod = args.TransportMethod;
        ServiceType serviceType = (ServiceType)packet.ReadByte();
        CommandType commandType = (CommandType)packet.ReadByte();

        if (services.TryGetValue(serviceType, out ClientService service))
        {
            service.ReceiveData(packet, serviceType, commandType, transportMethod);
        }
        else
        {
            Debug.LogWarning($"<color=red><b>CNS</b></color>: No service found for type {serviceType}. Command {commandType} will not be processed.");
        }
    }

    public override void Tick()
    {

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
