using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientLobby : MonoBehaviour
{
    public LobbyData LobbyData { get; internal set; } = new LobbyData();
    public UserData CurrentUser { get; internal set; } = new UserData();
    public ulong ClientTick { get; internal set; } = 0;

    private ITransportUtility transportUtility;
    private readonly ClientServiceUtility services = new ClientServiceUtility();
    private readonly ClientServiceUtility unconnectedServices = new ClientServiceUtility();

    internal RpcBus RpcBus { get; } = new RpcBus();

    internal void Init(ITransportUtility transport)
    {
        transportUtility = transport;

        foreach (var service in this.GetComponentsInChildren<ClientService>())
        {
            service.Init(this);
        }
    }

    internal void ReceiveData(NetPacket packet, TransportMethod? transportMethod)
    {
        ulong serviceId = packet.ReadULong();
        ushort commandType = packet.ReadUShort();
        if (services.GetService(serviceId, out ClientService service))
        {
            service.ReceiveData(packet, commandType, transportMethod);
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
        if (unconnectedServices.GetService(serviceId, out ClientService unconnectedService))
        {
            unconnectedService.ReceiveDataUnconnected(ipEndPoint, packet, commandType);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No unconnected service found for id {serviceId}.");
        }
    }

    internal void Tick()
    {
        ClientTick++;
    }

    public void SendToServer<T>(NetPacket packet, TransportMethod method) where T : ServerService
    {
        if (packet != null)
        {
            packet.Insert(0, NetResources.GenerateServerServiceId<T>());
            transportUtility.SendToAllRemotes(packet, method);
        }
    }

    public void SendToUnconnected<T>(IPEndPoint iPEndPoint, NetPacket packet) where T : ServerService
    {
        if (packet != null)
        {
            packet.Insert(0, NetResources.GenerateServerServiceId<T>());
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

    public bool RegisterService<T>(T service) where T : ClientService
    {
        return services.RegisterService(service);
    }

    public bool UnregisterService(ClientService service)
    {
        return services.UnregisterService(service.ServiceId);
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

    public bool UnregisterUnconnectedService(ClientService service)
    {
        return unconnectedServices.UnregisterService(service.ServiceId);
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

    public void RegisterRpcContainer(INetRpc rpcContainer)
    {
        RpcBus.RegisterRpcContainer(rpcContainer);
    }

    public void UnregisterRpcContainer(INetRpc rpcContainer)
    {
        RpcBus.UnregisterRpcContainer(rpcContainer);
    }
}
