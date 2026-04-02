using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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

    internal RpcBus RpcBus { get; } = new RpcBus();
    private readonly GameEventBus gameEventBus = new GameEventBus();

    internal void Init(ITransportUtility transportUtility, Scene? scene = null)
    {
        this.transportUtility = transportUtility;

        LobbyScene = scene;
        physicsScene = scene?.GetPhysicsScene();

        foreach (var service in this.GetComponentsInChildren<ServerService>())
        {
            service.Init(this);
        }
    }

    internal void ReceiveData(UserData user, NetPacket packet, TransportMethod? transportMethod)
    {
        ulong serviceId = packet.ReadULong();
        ushort commandType = packet.ReadUShort();
        if (services.GetService(serviceId, out ServerService service))
        {
            service.ReceiveData(user, packet, commandType, transportMethod);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No service found for id {serviceId}.");
        }
    }

    internal void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet)
    {
        ulong serviceId = packet.ReadULong();
        ushort commandType = packet.ReadUShort();
        if (unconnectedServices.GetService(serviceId, out ServerService unconnectedService))
        {
            unconnectedService.ReceiveDataUnconnected(ipEndPoint, packet, commandType);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No unconnected service found for id {serviceId}.");
        }
    }

    internal void UserJoined(UserData user)
    {
        user.PlayerId = GeneratePlayerId();
        services.UserJoined(user);
    }

    internal void UserJoinedGame(UserData user)
    {
        services.UserJoinedGame(user);
    }

    internal void UserLeft(UserData user)
    {
        services.UserLeft(user);
    }

    internal void Tick()
    {
        if (physicsScene.HasValue)
        {
            physicsScene.Value.Simulate(Time.fixedDeltaTime);
        }
        services.Tick();
        ServerTick++;
    }

    public void SendToLobby<T>(NetPacket packet, TransportMethod method, UserData exception = null) where T : ClientService
    {
        if (packet != null)
        {
            packet.Insert(0, NetResources.GenerateClientServiceId<T>());
            transportUtility.SendToRemotes(LobbyData.LobbyUsers.Where(u => u != exception).ToList().ConvertAll(user => user.UserId), packet, method);
        }
    }

    public void SendToGame<T>(NetPacket packet, TransportMethod method, UserData exception = null) where T : ClientService
    {
        if (packet != null)
        {
            packet.Insert(0, NetResources.GenerateClientServiceId<T>());
            transportUtility.SendToRemotes(LobbyData.GameUsers.Where(u => u != exception).ToList().ConvertAll(user => user.UserId), packet, method);
        }
    }

    public void SendToUser<T>(UserData user, NetPacket packet, TransportMethod method) where T : ClientService
    {
        if (packet != null)
        {
            packet.Insert(0, NetResources.GenerateClientServiceId<T>());
            transportUtility.SendToRemote(user.UserId, packet, method);
        }
    }

    public void SendToUnconnected<T>(IPEndPoint iPEndPoint, NetPacket packet) where T : ClientService
    {
        if (packet != null)
        {
            packet.Insert(0, NetResources.GenerateClientServiceId<T>());
            transportUtility.SendToUnconnectedRemote(iPEndPoint, packet);
        }
    }

    public void SendToUnconnected<T>(List<IPEndPoint> iPEndPoints, NetPacket packet) where T : ClientService
    {
        if (packet != null)
        {
            packet.Insert(0, NetResources.GenerateClientServiceId<T>());
            transportUtility.SendToUnconnectedRemotes(iPEndPoints, packet);
        }
    }

    public void BroadcastToUnconnected<T>(NetPacket packet) where T : ClientService
    {
        if (packet != null)
        {
            packet.Insert(0, NetResources.GenerateClientServiceId<T>());
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

    public bool RegisterService<T>(T service) where T : ServerService
    {
        return services.RegisterService(service);
    }

    public bool UnregisterService(ServerService service)
    {
        return services.UnregisterService(service.ServiceId);
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

    public bool UnregisterUnconnectedService(ServerService service)
    {
        return unconnectedServices.UnregisterService(service.ServiceId);
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

    public void RegisterRpcContainer(INetRpc rpcContainer)
    {
        RpcBus.RegisterRpcContainer(rpcContainer);
    }

    public void UnregisterRpcContainer(INetRpc rpcContainer)
    {
        RpcBus.UnregisterRpcContainer(rpcContainer);
    }

    public void RegisterGameEventListener(INetEvent listener)
    {
        gameEventBus.RegisterListener(listener);
    }

    public void UnregisterGameEventListener(INetEvent listener)
    {
        gameEventBus.UnregisterListener(listener);
    }

    public async Task<GameEventResult> TriggerGameEvent(GameEvent e)
    {
        return await gameEventBus.Fire(e);
    }

    internal byte GeneratePlayerId()
    {
        byte newPlayerId;
        do
        {
            newPlayerId = (byte)UnityEngine.Random.Range(0, byte.MaxValue);
        } while (LobbyData.LobbyUsers.Any(u => u.PlayerId == newPlayerId));
        return newPlayerId;
    }

    internal ushort GenerateObjectId()
    {
        ushort newObjectId;
        do
        {
            newObjectId = (ushort)UnityEngine.Random.Range(byte.MaxValue, ushort.MaxValue);
        } while (GetService<ObjectServerService>().ServerObjects.ContainsKey(newObjectId));
        return newObjectId;
    }
}
