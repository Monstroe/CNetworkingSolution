using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ServerLobby : MonoBehaviour
{
    public LobbyData LobbyData { get; protected set; } = new LobbyData();
    public ServerLobbyGameData GameData { get; private set; } = new ServerLobbyGameData();
    public Map Map { get; private set; }

    protected List<NetTransport> transports;
    private Dictionary<ServiceType, ServerService> services = new Dictionary<ServiceType, ServerService>();

    public void Init(int lobbyId, List<NetTransport> transports)
    {
        LobbyData.LobbyId = lobbyId;
        this.transports = transports;

        // Init Server Services (ADD NEW SERVICES HERE)
        services.Add(ServiceType.LOBBY, new GameObject("LobbyServerService").AddComponent<LobbyServerService>());
        services.Add(ServiceType.GAME, new GameObject("GameServerService").AddComponent<GameServerService>());
        services.Add(ServiceType.OBJECT, new GameObject("ObjectServerService").AddComponent<ObjectServerService>());
        services.Add(ServiceType.PLAYER, new GameObject("PlayerServerService").AddComponent<PlayerServerService>());
        services.Add(ServiceType.FX, new GameObject("FXServerService").AddComponent<FXServerService>());
        services.Add(ServiceType.MAP, new GameObject("MapServerService").AddComponent<MapServerService>());
        services.Add(ServiceType.CHAT, new GameObject("ChatServerService").AddComponent<ChatServerService>());

        foreach (var service in services.Values)
        {
            service.transform.SetParent(transform);
        }

        // Init Map
        Map = Instantiate(Resources.Load<GameObject>("Prefabs/Map"), this.transform).GetComponent<Map>();
        foreach (Renderer r in Map.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
        foreach (Collider c in Map.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
        foreach (ClientObject obj in Map.GetComponentsInChildren<ClientObject>(true))
            obj.enabled = false;
    }

    public void SendToLobby(NetPacket packet, TransportMethod method, UserData exception = null)
    {
        SendToUsers(LobbyData.LobbyUsers.Where(u => u != exception).ToList(), packet, method);
    }

    public void SendToGame(NetPacket packet, TransportMethod method, UserData exception = null)
    {
        SendToUsers(LobbyData.LobbyUsers.Where(u => u.InGame && u != exception).ToList(), packet, method);
    }

    public void SendToUser(UserData user, NetPacket packet, TransportMethod method)
    {
        var (userId, transportIndex) = ServerManager.Instance.GetUserIdAndTransportIndex(user);
        transports[transportIndex].Send(userId, packet, method);
    }

    public void SendToUsers(List<UserData> users, NetPacket packet, TransportMethod method)
    {
        Dictionary<NetTransport, List<uint>> userDict = new Dictionary<NetTransport, List<uint>>();
        foreach (var user in users)
        {
            var (userId, transportIndex) = ServerManager.Instance.GetUserIdAndTransportIndex(user);
            if (!userDict.ContainsKey(transports[transportIndex]))
            {
                userDict[transports[transportIndex]] = new List<uint>();
            }
            userDict[transports[transportIndex]].Add(userId);
        }

        foreach (var (transport, userIds) in userDict)
        {
            transport.SendToList(userIds, packet, method);
        }
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

    public void UserJoinedGame(UserData user)
    {
        user.InGame = true;
        SendToLobby(PacketBuilder.GameUserJoined(user), TransportMethod.Reliable);
        foreach (var service in services.Values)
        {
            service.UserJoinedGame(this, user);
        }
    }

    public void UserLeft(UserData user)
    {
        foreach (var service in services.Values)
        {
            service.UserLeft(this, user);
        }
    }

    public void Tick()
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
            newPlayerId = (byte)UnityEngine.Random.Range(0, LobbyData.Settings.MaxUsers);
        } while (LobbyData.LobbyUsers.Any(u => u.PlayerId == newPlayerId));
        return newPlayerId;
    }

    public ushort GenerateObjectId()
    {
        ushort newObjectId;
        do
        {
            newObjectId = (ushort)UnityEngine.Random.Range(LobbyData.Settings.MaxUsers, ushort.MaxValue);
        } while (GameData.ServerObjects.ContainsKey(newObjectId));
        return newObjectId;
    }
}
