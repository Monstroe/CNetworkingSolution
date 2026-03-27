using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientLobby : MonoBehaviour
{
    public LobbyData LobbyData { get; set; } = new LobbyData();
    public UserData CurrentUser { get; set; } = new UserData();
    public ulong ClientTick { get; set; } = 0;

    private ITransportUtility transportUtility;
    private readonly ClientServiceUtility services = new ClientServiceUtility();
    private readonly ClientServiceUtility unconnectedServices = new ClientServiceUtility();

    public void Init(ITransportUtility transport)
    {
        transportUtility = transport;

        foreach (var service in this.GetComponentsInChildren<ClientService>())
        {
            service.Init(this);
        }
    }

    void FixedUpdate()
    {
        ClientTick++;
    }

    public void SendToServer(NetPacket packet, TransportMethod method)
    {
        if (packet != null)
        {
            transportUtility.SendToAllRemotes(packet, method);
        }
    }

    public void SendToUnconnected(IPEndPoint iPEndPoint, NetPacket packet)
    {
        if (packet != null)
        {
            transportUtility.SendToUnconnectedRemote(iPEndPoint, packet);
        }
    }

    public void SendToUnconnected(List<IPEndPoint> iPEndPoints, NetPacket packet)
    {
        if (packet != null)
        {
            transportUtility.SendToUnconnectedRemotes(iPEndPoints, packet);
        }
    }

    public void BroadcastToUnconnected(NetPacket packet)
    {
        if (packet != null)
        {
            transportUtility.BroadcastToUnconnectedRemotes(packet);
        }
    }

    public void DisconnectFromLobby()
    {
        transportUtility.DisconnectTransports();
    }

#if !CNS_LOBBY_SINGLE || (CNS_LOBBY_SINGLE && CNS_SYNC_HOST)
    public void KickUser(UserData user, string reason)
    {
        if (user.UserId == CurrentUser.UserId)
        {
            Debug.LogWarning("You cannot kick yourself from the lobby.");
            return;
        }

        SendToServer(LobbyClientService.LobbyUserKick(user, reason), TransportMethod.Reliable);
    }
#endif

    public void ReceiveData(NetPacket packet, TransportMethod? transportMethod)
    {
        uint serviceId = packet.ReadUInt();
        CommandType commandType = (CommandType)packet.ReadByte();

        if (services.GetService(serviceId, out ClientService service))
        {
            service.ReceiveData(packet, commandType, transportMethod);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No service found for id {serviceId}. Command {commandType} will not be processed.");
        }
    }

    public void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet)
    {
        uint serviceId = packet.ReadUInt();
        CommandType commandType = (CommandType)packet.ReadByte();

        if (unconnectedServices.GetService(serviceId, out ClientService unconnectedService))
        {
            unconnectedService.ReceiveDataUnconnected(ipEndPoint, packet, commandType);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No unconnected service found for id {serviceId}. Command {commandType} will not be processed.");
        }
    }

    public bool RegisterService<T>(T service) where T : ClientService
    {
        return services.RegisterService(service);
    }

    public bool UnregisterService<T>() where T : ClientService
    {
        return services.UnregisterService<T>(out _);
    }

    public T GetService<T>() where T : ClientService
    {
        ClientService service = services.GetService<T>(out ulong serviceId);
        if (service != null)
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService with id {serviceId} not found.");
            return null;
        }
    }

    public bool RegisterUnconnectedService<T>(T service) where T : ClientService
    {
        return unconnectedServices.RegisterService(service);
    }

    public bool UnregisterUnconnectedService<T>() where T : ClientService
    {
        return unconnectedServices.UnregisterService<T>(out _);
    }

    public T GetUnconnectedService<T>() where T : ClientService
    {
        ClientService service = unconnectedServices.GetService<T>(out ulong serviceId);
        if (service != null)
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ClientService with id {serviceId} not found.");
            return null;
        }
    }
}
