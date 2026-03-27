using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerLobby : MonoBehaviour
{
    public LobbyData LobbyData { get; private set; } = new LobbyData();
    public ulong ServerTick { get; private set; } = 0;
    public Scene? LobbyScene { get; private set; }

    private PhysicsScene? physicsScene;

    private ITransportUtility transportUtility;
    private readonly ServerServiceUtility services = new ServerServiceUtility();
    private readonly ServerServiceUtility unconnectedServices = new ServerServiceUtility();

    public void Init(ITransportUtility transportUtility, Scene? scene = null)
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

    public void KickUser(UserData user)
    {
        transportUtility.KickRemote(user.UserId);
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
        uint serviceId = packet.ReadUInt();
        CommandType commandType = (CommandType)packet.ReadByte();

        if (services.GetService(serviceId, out ServerService service))
        {
            service.ReceiveData(user, packet, commandType, transportMethod);
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

        if (unconnectedServices.GetService(serviceId, out ServerService unconnectedService))
        {
            unconnectedService.ReceiveDataUnconnected(ipEndPoint, packet, commandType);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No unconnected service found for id {serviceId}. Command {commandType} will not be processed.");
        }
    }

    public void UserJoined(UserData user)
    {
        user.PlayerId = GeneratePlayerId();
        services.UserJoined(user);
    }

    public void UserJoinedGame(UserData user)
    {
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

    public bool RegisterService<T>(T service) where T : ServerService
    {
        return services.RegisterService(service);
    }

    public bool UnregisterService<T>() where T : ServerService
    {
        return services.UnregisterService<T>(out _);
    }

    public T GetService<T>() where T : ServerService
    {
        ServerService service = services.GetService<T>(out ulong serviceId);
        if (service != null)
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService with id {serviceId} not found.");
            return null;
        }
    }

    public bool RegisterUnconnectedService<T>(T service) where T : ServerService
    {
        return unconnectedServices.RegisterService(service);
    }

    public bool UnregisterUnconnectedService<T>() where T : ServerService
    {
        return unconnectedServices.UnregisterService<T>(out _);
    }

    public T GetUnconnectedService<T>() where T : ServerService
    {
        ServerService service = unconnectedServices.GetService<T>(out ulong serviceId);
        if (service != null)
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ServerService with id {serviceId} not found.");
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
