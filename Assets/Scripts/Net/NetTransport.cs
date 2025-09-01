using System.Collections.Generic;
using UnityEngine;

public abstract class NetTransport : MonoBehaviour
{
    public delegate void NetworkConnectedHandler(NetTransport transport, ConnectedArgs args);
    /// <summary>
    /// Event triggered when the user is connected. This is called when the client connects to the server or when the server has a client connect to it.
    /// </summary>
    public event NetworkConnectedHandler OnNetworkConnected;

    public delegate void NetworkDisconnectedHandler(NetTransport transport, DisconnectedArgs args);
    /// <summary>
    /// Event triggered when the user is disconnected. This is called when the client disconnects from the server or when the server has a client disconnect from it.
    /// </summary>
    public event NetworkDisconnectedHandler OnNetworkDisconnected;

    public delegate void NetworkReceivedHandler(NetTransport transport, ReceivedArgs args);
    /// <summary>
    /// Event triggered when a packet is received. This is called when the client receives a packet from the server or when the server receives a packet from a client.
    /// </summary>
    public event NetworkReceivedHandler OnNetworkReceived;

    public NetDeviceType HostType => hostType;
    public virtual uint ServerClientId { get; }
    public virtual List<uint> ConnectedClientIds { get; }

    protected NetDeviceType hostType = NetDeviceType.None;

    public abstract void Initialize();
    public abstract bool StartClient();
    public abstract bool StartServer();
    public abstract void Send(uint remoteId, NetPacket packet, TransportMethod method);
    public abstract void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod method);
    public abstract void SendToAll(NetPacket packet, TransportMethod method);
    public abstract void Disconnect();
    public abstract void DisconnectRemote(uint remoteId);
    public abstract void Shutdown();

    public void RaiseNetworkConnected(uint remoteId)
    {
        var args = new ConnectedArgs { RemoteId = remoteId };
        OnNetworkConnected?.Invoke(this, args);
    }

    public void RaiseNetworkDisconnected(uint remoteId)
    {
        var args = new DisconnectedArgs { RemoteId = remoteId };
        OnNetworkDisconnected?.Invoke(this, args);
    }

    public void RaiseNetworkReceived(uint remoteId, NetPacket receivedPacket, TransportMethod? method)
    {
        var args = new ReceivedArgs { RemoteId = remoteId, Packet = receivedPacket, TransportMethod = method };
        OnNetworkReceived?.Invoke(this, args);
    }
}

public class ConnectedArgs
{
    public uint RemoteId { get; set; }
}

public class DisconnectedArgs
{
    public uint RemoteId { get; set; }
}

public class ReceivedArgs
{
    public uint RemoteId { get; set; }
    public NetPacket Packet { get; set; }
    public TransportMethod? TransportMethod { get; set; }
}

public enum NetDeviceType
{
    None,
    Server,
    Client
}

public enum TransportMethod
{
    Reliable,
    ReliableUnordered,
    UnreliableSequenced,
    Unreliable,
}

public enum TransportType
{
#if CNS_TRANSPORT_LOCAL
    Local,
#endif
#if CNS_TRANSPORT_LITENETLIB
    LiteNetLib,
#endif
#if CNS_TRANSPORT_STEAMWORKS && CNS_HOST_AUTH
    SteamWorks,
#endif
}
