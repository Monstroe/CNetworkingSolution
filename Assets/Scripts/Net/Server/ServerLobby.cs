using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ServerLobby : Lobby
{
    private Dictionary<ServiceType, ServerService> services = new Dictionary<ServiceType, ServerService>();

    public override void Init(int lobbyId, NetTransport transport)
    {
        base.Init(lobbyId, transport);

        // Init Server Services
        services.Add(ServiceType.LOBBY, new GameObject("LobbyServerService").AddComponent<LobbyServerService>());
        services.Add(ServiceType.CHAT, new GameObject("ChatServerService").AddComponent<ChatServerService>());

        foreach (var service in services.Values)
        {
            service.transform.SetParent(transform);
        }
    }

    public void SendToRoom(NetPacket packet, TransportMethod method, UserData exception = null)
    {
        List<uint> userIds = new List<uint>();
        foreach (var user in LobbyData.LobbyUsers)
        {
            if (user != exception)
            {
                userIds.Add(user.UserId);
            }
        }

        transport.SendToList(userIds, packet, method);
    }

    public void SendToUser(UserData user, NetPacket packet, TransportMethod method)
    {
        transport.Send(user.UserId, packet, method);
    }

    public void SendToUsers(List<UserData> users, NetPacket packet, TransportMethod method)
    {
        transport.SendToList(users.Select(u => u.UserId).ToList(), packet, method);
    }

    public void ReceiveData(UserData user, NetPacket packet, TransportMethod? transportMethod)
    {
        ServiceType serviceType = (ServiceType)packet.ReadByte();
        CommandType commandType = (CommandType)packet.ReadByte();

        if (services.TryGetValue(serviceType, out ServerService service))
        {
            service.ReceiveData(this, user, packet, serviceType, commandType, transportMethod);
        }
        else
        {
            Debug.LogWarning($"<color=red><b>CNS</b></color>: No service found for type {serviceType}. Command {commandType} will not be processed.");
        }
    }

    public void UserJoined(UserData user)
    {
        user.PlayerId = GeneratePlayerId();

        foreach (var service in services.Values)
        {
            service.UserJoined(this, user);
        }
    }

    public void UserLeft(UserData user)
    {
        foreach (var service in services.Values)
        {
            service.UserLeft(this, user);
        }
    }

    public override void Tick()
    {
        foreach (var service in services.Values)
        {
            service.Tick(this);
        }
    }

    public ServerService GetService(ServiceType serviceType)
    {
        if (services.TryGetValue(serviceType, out ServerService service))
        {
            return service;
        }
        else
        {
            Debug.LogWarning($"<color=red><b>CNS</b></color>: ServerService {serviceType} not found.");
            return null;
        }
    }

    private byte GeneratePlayerId()
    {
        byte newPlayerId;
        do
        {
            newPlayerId = (byte)UnityEngine.Random.Range(0, 256);
        } while (!LobbyData.LobbyUsers.Any(u => u.PlayerId == newPlayerId));
        return newPlayerId;
    }
}
