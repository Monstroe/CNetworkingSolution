using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerLobby : MonoBehaviour
{
    public LobbyData LobbyData { get; private set; } = new LobbyData();
    public ulong ServerTick { get; private set; } = 0;
    public Scene? LobbyScene { get; private set; }

    private PhysicsScene? physicsScene;

    private MultiTransportUtility transportUtility;
    private readonly ServerServiceUtility services = new ServerServiceUtility();

    public void Init(MultiTransportUtility transportUtility, Scene? scene = null)
    {
        this.transportUtility = transportUtility;

        LobbyScene = scene;
        physicsScene = scene?.GetPhysicsScene();

        foreach (var service in this.GetComponentsInChildren<ServerService>())
        {
            service.Init(this);
        }
    }

    public void SendToLobby(NetPacket packet, TransportMethod method, UserData exception = null)
    {
        if (packet != null)
        {
            transportUtility.SendToRemotes(LobbyData.LobbyUsers.Where(u => u != exception).ToList().ConvertAll(user => user.UserId), packet, method);
        }
    }

    public void SendToGame(NetPacket packet, TransportMethod method, UserData exception = null)
    {
        if (packet != null)
        {
            transportUtility.SendToRemotes(LobbyData.GameUsers.Where(u => u != exception).ToList().ConvertAll(user => user.UserId), packet, method);
        }
    }

    public void SendToUser(UserData user, NetPacket packet, TransportMethod method)
    {
        if (packet != null)
        {
            transportUtility.SendToRemote(user.UserId, packet, method);
        }
    }

    public void ShutdownLobby()
    {
        foreach (var user in LobbyData.LobbyUsers.ToList())
        {
            transportUtility.KickRemote(user.UserId);
        }
    }

    public void ReceiveData(UserData user, NetPacket packet, TransportMethod? transportMethod)
    {
        ServiceType serviceType = (ServiceType)packet.ReadByte();
        CommandType commandType = (CommandType)packet.ReadByte();

        if (services.GetService(serviceType, out ServerService service))
        {
            service.ReceiveData(user, packet, serviceType, commandType, transportMethod);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No service found for type {serviceType}. Command {commandType} will not be processed.");
        }
    }

    public void UserJoined(UserData user)
    {
        user.PlayerId = GeneratePlayerId();
        services.UserJoined(user);
    }

    public void UserJoinedGame(UserData user)
    {
        user.InGame = true;
        SendToLobby(PacketBuilder.GameUserJoined(user), TransportMethod.Reliable);
        services.UserJoinedGame(user);
    }

    public void UserLeft(UserData user)
    {
        services.UserLeft(user);
    }

    public void Tick()
    {
        if (physicsScene.HasValue)
        {
            physicsScene.Value.Simulate(Time.fixedDeltaTime);
        }
        services.Tick();
        ServerTick++;
    }

    public void RegisterService<T>(T service) where T : ServerService
    {
        if (services.RegisterService(service))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Registered ServerService {typeof(T)}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {typeof(T)} is already registered.");
        }
    }

    public void UnregisterService<T>()
    {
        if (services.UnregisterService<T>())
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Unregistered ServerService {typeof(T)}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {typeof(T)} is not registered.");
        }
    }

    public T GetService<T>() where T : ServerService
    {
        ServerService service = services.GetService<T>();
        if (service != null)
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {typeof(T)} not found.");
            return null;
        }
    }

    public byte GeneratePlayerId()
    {
        byte newPlayerId;
        do
        {
            newPlayerId = (byte)UnityEngine.Random.Range(0, byte.MaxValue);
        } while (LobbyData.LobbyUsers.Any(u => u.PlayerId == newPlayerId));
        return newPlayerId;
    }

    public ushort GenerateObjectId()
    {
        ushort newObjectId;
        do
        {
            newObjectId = (ushort)UnityEngine.Random.Range(byte.MaxValue, ushort.MaxValue);
        } while (GetService<ObjectServerService>().ServerObjects.ContainsKey(newObjectId));
        return newObjectId;
    }
}
