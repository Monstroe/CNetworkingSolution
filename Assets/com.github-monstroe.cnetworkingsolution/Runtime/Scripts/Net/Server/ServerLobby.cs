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

    public void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet)
    {
        ServiceType serviceType = (ServiceType)packet.ReadByte();
        CommandType commandType = (CommandType)packet.ReadByte();

        if (unconnectedServices.GetService(serviceType, out ServerService unconnectedService))
        {
            unconnectedService.ReceiveDataUnconnected(ipEndPoint, packet, serviceType, commandType);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No unconnected service found for type {serviceType}. Command {commandType} will not be processed.");
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

    public void RegisterService<T>(T service) where T : ServerService
    {
        if (services.RegisterService(service))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Registered ServerService {service.ServiceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {service.ServiceType} is already registered.");
        }
    }

    public void UnregisterService<T>() where T : ServerService
    {
        if (services.UnregisterService<T>(out ServiceType serviceType))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Unregistered ServerService {serviceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {serviceType} is not registered.");
        }
    }

    public T GetService<T>() where T : ServerService
    {
        ServerService service = services.GetService<T>(out ServiceType serviceType);
        if (service != null)
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {serviceType} not found.");
            return null;
        }
    }

    public void RegisterUnconnectedService<T>(T service) where T : ServerService
    {
        if (unconnectedServices.RegisterService(service))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Registered unconnected ServerService {service.ServiceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ServerService {service.ServiceType} is already registered.");
        }
    }

    public void UnregisterUnconnectedService<T>() where T : ServerService
    {
        if (unconnectedServices.UnregisterService<T>(out ServiceType serviceType))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Unregistered unconnected ServerService {serviceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ServerService {serviceType} is not registered.");
        }
    }

    public T GetUnconnectedService<T>() where T : ServerService
    {
        ServerService service = unconnectedServices.GetService<T>(out ServiceType serviceType);
        if (service != null)
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ServerService {serviceType} not found.");
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
