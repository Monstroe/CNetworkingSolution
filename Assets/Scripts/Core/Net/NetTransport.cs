using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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

    public delegate void HandleNetworkReceivedUnconnectedHandler(NetTransport transport, ReceivedUnconnectedArgs args);
    /// <summary>
    /// Event triggered when an unconnected packet is received. This is called when a packet is received from an unconnected source.
    /// </summary>
    public event HandleNetworkReceivedUnconnectedHandler OnNetworkReceivedUnconnected;

    public delegate void NetworkErrorHandler(NetTransport transport, ErrorArgs args);
    /// <summary>
    /// Event triggered when a network error occurs.
    /// </summary>
    public event NetworkErrorHandler OnNetworkError;

    public TransportData TransportData { get; protected set; } = new TransportData();
    protected bool initialized = false;

    public abstract void Initialize(NetDeviceType deviceType);

#nullable enable
    public virtual bool StartDevice(TransportSettings? transportSettings = null)
    {
        if (TransportData.DeviceType == NetDeviceType.Client)
        {
            return StartClient(transportSettings);
        }
        else if (TransportData.DeviceType == NetDeviceType.Server)
        {
            return StartServer(transportSettings);
        }
        else
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Device type not set. Cannot start transport.");
            return false;
        }
    }

    protected abstract bool StartClient(TransportSettings? transportSettings = null);
    protected abstract bool StartServer(TransportSettings? transportSettings = null);
#nullable disable
    public abstract void Send(uint remoteId, NetPacket packet, TransportMethod method);
    public abstract void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod method);
    public abstract void SendToAll(NetPacket packet, TransportMethod method);
    public abstract void SendUnconnected(IPEndPoint ipEndPoint, NetPacket packet);
    public abstract void SendToListUnconnected(List<IPEndPoint> ipEndPoints, NetPacket packet);
    public abstract void BroadcastUnconnected(NetPacket packet);
    public abstract void Disconnect();
    public abstract void DisconnectRemote(uint remoteId);
    public abstract void Shutdown();

    public void RaiseNetworkConnected(uint remoteId)
    {
        var args = new ConnectedArgs { RemoteId = remoteId };
        OnNetworkConnected?.Invoke(this, args);
    }

    public void RaiseNetworkDisconnected(uint remoteId, TransportCode code)
    {
        var args = new DisconnectedArgs { RemoteId = remoteId, Code = code };
        OnNetworkDisconnected?.Invoke(this, args);
    }

    public void RaiseNetworkReceived(uint remoteId, NetPacket receivedPacket, TransportMethod? method)
    {
        var args = new ReceivedArgs { RemoteId = remoteId, Packet = receivedPacket, TransportMethod = method };
        OnNetworkReceived?.Invoke(this, args);
    }

    public void RaiseNetworkReceivedUnconnected(IPEndPoint ipEndPoint, NetPacket packet)
    {
        var args = new ReceivedUnconnectedArgs { IPEndPoint = ipEndPoint, Packet = packet };
        OnNetworkReceivedUnconnected?.Invoke(this, args);
    }

    public void RaiseNetworkError(TransportCode errorCode, SocketError? socketError = null)
    {
        var args = new ErrorArgs { Code = errorCode, SocketError = socketError };
        OnNetworkError?.Invoke(this, args);
    }
}

public class ConnectedArgs
{
    public uint RemoteId { get; set; }
}

public class DisconnectedArgs
{
    public uint RemoteId { get; set; }
    public TransportCode Code { get; set; }
}

public class ReceivedArgs
{
    public uint RemoteId { get; set; }
    public NetPacket Packet { get; set; }
    public TransportMethod? TransportMethod { get; set; }
}

public class ReceivedUnconnectedArgs
{
    public IPEndPoint IPEndPoint { get; set; }
    public NetPacket Packet { get; set; }
}

public class ErrorArgs
{
    public TransportCode Code { get; set; }
    public SocketError? SocketError { get; set; }
}

public enum TransportMethod
{
    Reliable,
    ReliableUnordered,
    UnreliableSequenced,
    Unreliable,
}

public enum TransportCode
{
    ConnectionClosed,
    ConnectionFailed,
    ConnectionRejected,
    ConnectionLost,
    InvalidData,
    SocketError,
    UnknownError,
    // Additional reasons can be added here
}
