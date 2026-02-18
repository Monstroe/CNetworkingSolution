#if CNS_TRANSPORT_LITENETLIB
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;

public class LiteNetLibTransport : NetTransport, INetEventListener, IDeliveryEventListener
{
    [Tooltip("The port to listen on (if server) or connect to (if client)")]
    [SerializeField] private ushort port = 8888;
    [Tooltip("The address to connect to as client; ignored if server")]
    [SerializeField] private string address = "127.0.0.1";
    [Tooltip("The key used to successfully connect to the server")]
    [SerializeField] private string connectionKey = "Bruh-Wizz-Arcgis";
    [Tooltip("Interval between ping packets used for detecting latency and checking connection, in seconds")]
    [SerializeField] private float pingInterval = 1f;
    [Tooltip("Maximum duration for a connection to survive without receiving packets, in seconds")]
    [SerializeField] private float disconnectTimeout = 5f;
    [Tooltip("Delay between connection attempts, in seconds")]
    [SerializeField] private float reconnectDelay = 0.5f;
    [Tooltip("Maximum connection attempts before client stops and reports a disconnection")]
    [SerializeField] private int maxConnectAttempts = 10;
    [Tooltip("Simulated chance for a packet to be \"lost\", from 0 (no simulation) to 100 percent")]
    [SerializeField] private int simulatePacketLossChance = 0;
    [Tooltip("Simulated minimum additional latency for packets in milliseconds (0 for no simulation)")]
    [SerializeField] private int simulateMinLatency = 0;
    [Tooltip("Simulated maximum additional latency for packets in milliseconds (0 for no simulation)")]
    [SerializeField] private int simulateMaxLatency = 0;
    [Tooltip("Enable messages receiving without connection")]
    [SerializeField] private bool unconnectedMessagesEnabled = false;
    [Tooltip("Allows receive broadcast packets")]
    [SerializeField] private bool broadcastReceiveEnabled = false;

    protected NetManager netManager;
    protected readonly Dictionary<uint, NetPeer> connectedPeers = new Dictionary<uint, NetPeer>();
    private readonly Dictionary<uint, byte[]> peerMessageBuffers = new Dictionary<uint, byte[]>();

    void FixedUpdate()
    {
        netManager?.PollEvents();
    }

    public override void Initialize(NetDeviceType deviceType)
    {
        TransportData.DeviceType = deviceType;

        netManager = new NetManager(this)
        {
            PingInterval = SecondsToMilliseconds(pingInterval),
            DisconnectTimeout = SecondsToMilliseconds(disconnectTimeout),
            ReconnectDelay = SecondsToMilliseconds(reconnectDelay),
            MaxConnectAttempts = maxConnectAttempts,
            SimulatePacketLoss = simulatePacketLossChance > 0,
            SimulationPacketLossChance = simulatePacketLossChance,
            SimulateLatency = simulateMaxLatency > 0,
            SimulationMinLatency = simulateMinLatency,
            SimulationMaxLatency = simulateMaxLatency,
            UnconnectedMessagesEnabled = unconnectedMessagesEnabled,
            BroadcastReceiveEnabled = broadcastReceiveEnabled
        };
    }

#nullable enable
    protected override bool StartClient(TransportSettings? transportSettings = null)
    {
        if (initialized)
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Already started as " + TransportData.DeviceType);
            return false;
        }

        initialized = true;

        if (transportSettings != null)
        {
            address = transportSettings.ConnectionAddress ?? address;
            port = transportSettings.ConnectionPort ?? port;
            connectionKey = transportSettings.ConnectionKey ?? connectionKey;
            unconnectedMessagesEnabled = transportSettings.UnconnectedPacketsEnabled ?? unconnectedMessagesEnabled;
            broadcastReceiveEnabled = transportSettings.UnconnectedPacketsEnabled ?? broadcastReceiveEnabled;
        }

        var success = netManager.Start();
        if (!success)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Failed to start LiteNetLib transport.");
            return false;
        }

        netManager.UnconnectedMessagesEnabled = unconnectedMessagesEnabled;
        netManager.BroadcastReceiveEnabled = broadcastReceiveEnabled;
        netManager.Connect(address, port, connectionKey);
        return true;
    }

    protected override bool StartServer(TransportSettings? transportSettings = null)
    {
        if (initialized)
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Already started as " + TransportData.DeviceType);
            return false;
        }

        initialized = true;

        if (transportSettings != null)
        {
            address = transportSettings.ConnectionAddress ?? address;
            port = transportSettings.ConnectionPort ?? port;
            connectionKey = transportSettings.ConnectionKey ?? connectionKey;
            unconnectedMessagesEnabled = transportSettings.UnconnectedPacketsEnabled ?? unconnectedMessagesEnabled;
            broadcastReceiveEnabled = transportSettings.UnconnectedPacketsEnabled ?? broadcastReceiveEnabled;
        }

        netManager.UnconnectedMessagesEnabled = unconnectedMessagesEnabled;
        netManager.BroadcastReceiveEnabled = broadcastReceiveEnabled;
        bool success = netManager.Start(port);
        if (!success)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Failed to start LiteNetLib transport.");
            return false;
        }

        return true;
    }
#nullable disable

    public override void Send(uint remoteId, NetPacket packet, TransportMethod protocol)
    {
        if (connectedPeers.TryGetValue(remoteId, out NetPeer peer))
        {
            SendInternal(peer, packet.ByteArray, protocol);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to send to a peer that is not connected: {remoteId}");
        }
    }

    public override void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod protocol)
    {
        byte[] data = packet.ByteArray;
        foreach (var remoteId in remoteIds)
        {
            if (connectedPeers.TryGetValue(remoteId, out NetPeer peer))
            {
                SendInternal(peer, data, protocol);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to send to a peer that is not connected: {remoteId}");
            }
        }
    }

    public override void SendToAll(NetPacket packet, TransportMethod protocol)
    {
        var deliveryMethod = ConvertProtocol(protocol);
        netManager.SendToAll(packet.ByteArray, 0, deliveryMethod);
    }

    private void SendInternal(NetPeer peer, byte[] data, TransportMethod protocol)
    {
        var deliveryMethod = ConvertProtocol(protocol);
        peerMessageBuffers[(uint)peer.Id] = data;
        if (deliveryMethod == DeliveryMethod.ReliableOrdered || deliveryMethod == DeliveryMethod.ReliableUnordered)
        {
            peer.SendWithDeliveryEvent(data, 0, deliveryMethod, null);
        }
        else
        {
            peer.Send(data, 0, deliveryMethod);
        }
    }

    public override void SendUnconnected(IPEndPoint ipEndPoint, NetPacket packet)
    {
        if (netManager.UnconnectedMessagesEnabled)
        {
            netManager.SendUnconnectedMessage(packet.ByteArray, ipEndPoint);
        }
    }

    public override void SendToListUnconnected(List<IPEndPoint> ipEndPoints, NetPacket packet)
    {
        if (netManager.UnconnectedMessagesEnabled)
        {
            byte[] data = packet.ByteArray;
            foreach (var ipEndPoint in ipEndPoints)
            {
                netManager.SendUnconnectedMessage(data, ipEndPoint);
            }
        }
    }

    public override void BroadcastUnconnected(NetPacket packet)
    {
        if (netManager.BroadcastReceiveEnabled)
        {
            netManager.SendBroadcast(packet.ByteArray, port);
        }
    }

    public override void Disconnect()
    {
        netManager.DisconnectAll();
    }

    public override void DisconnectRemote(uint remoteId)
    {
        if (connectedPeers.TryGetValue(remoteId, out NetPeer peer))
        {
            if (peerMessageBuffers.TryGetValue(remoteId, out byte[] lastData))
            {
                peer.Disconnect(lastData);
            }
            else
            {
                peer.Disconnect();
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to disconnect a peer that is not connected: {remoteId}");
        }
    }

    public override void Shutdown()
    {
        if (netManager != null)
        {
            Disconnect();
            netManager.Stop();
        }

        initialized = false;
    }

    private DeliveryMethod ConvertProtocol(TransportMethod protocol)
    {
        switch (protocol)
        {
            case TransportMethod.Unreliable:
                {
                    return DeliveryMethod.Unreliable;
                }
            case TransportMethod.UnreliableSequenced:
                {
                    return DeliveryMethod.Sequenced;
                }
            case TransportMethod.Reliable:
                {
                    return DeliveryMethod.ReliableOrdered;
                }
            case TransportMethod.ReliableUnordered:
                {
                    return DeliveryMethod.ReliableUnordered;
                }
            default:
                {
                    throw new ArgumentOutOfRangeException("<color=red><b>CNS</b></color>: Unknown protocol: " + protocol);
                }
        }
    }

    protected TransportMethod ConvertProtocolBack(DeliveryMethod method)
    {
        switch (method)
        {
            case DeliveryMethod.Unreliable:
                {
                    return TransportMethod.Unreliable;
                }
            case DeliveryMethod.Sequenced:
                {
                    return TransportMethod.UnreliableSequenced;
                }
            case DeliveryMethod.ReliableOrdered:
                {
                    return TransportMethod.Reliable;
                }
            case DeliveryMethod.ReliableUnordered:
                {
                    return TransportMethod.ReliableUnordered;
                }
            default:
                {
                    throw new ArgumentOutOfRangeException("<color=red><b>CNS</b></color>: Unknown protocol: " + method);
                }
        }
    }

    protected TransportCode ConvertCode(DisconnectReason disconnectReason)
    {
        switch (disconnectReason)
        {
            case DisconnectReason.ConnectionFailed:
                return TransportCode.ConnectionFailed;
            case DisconnectReason.Timeout:
                return TransportCode.ConnectionLost;
            case DisconnectReason.HostUnreachable:
                return TransportCode.ConnectionFailed;
            case DisconnectReason.NetworkUnreachable:
                return TransportCode.ConnectionFailed;
            case DisconnectReason.RemoteConnectionClose:
                return TransportCode.ConnectionClosed;
            //case DisconnectReason.DisconnectPeerCalled:
            case DisconnectReason.ConnectionRejected:
                return TransportCode.ConnectionRejected;
            case DisconnectReason.InvalidProtocol:
                return TransportCode.InvalidData;
            //case DisconnectReason.UnknownHost:
            //case DisconnectReason.Reconnect:
            //case DisconnectReason.PeerToPeerConnection:
            case DisconnectReason.PeerNotFound:
                return TransportCode.ConnectionFailed;
            default:
                return TransportCode.UnknownError;
        }
    }

    void IDeliveryEventListener.OnMessageDelivered(NetPeer peer, object userData)
    {
        peerMessageBuffers.Remove((uint)peer.Id);
    }

    protected virtual void ConnectionRequested(ConnectionRequest request)
    {
        request.AcceptIfKey(connectionKey);
    }

    protected virtual void ConnectPeer(NetPeer peer)
    {
        var peerId = (uint)peer.Id;

        if (!connectedPeers.ContainsKey(peerId))
        {
            connectedPeers[peerId] = peer;
            TransportData.ConnectedClientIds.Add(peerId);
            RaiseNetworkConnected(peerId);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to connect a peer that is already connected: {peerId}");
        }
    }

    protected virtual void DisconnectPeer(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (disconnectInfo.AdditionalData.AvailableBytes > 0)
        {
            ReceiveData(peer, disconnectInfo.AdditionalData, DeliveryMethod.ReliableOrdered);
        }

        var peerId = (uint)peer.Id;
        if (connectedPeers.Remove(peerId))
        {
            TransportData.ConnectedClientIds.Remove(peerId);
            RaiseNetworkDisconnected(peerId, ConvertCode(disconnectInfo.Reason));
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to disconnect a peer that is not connected: {peerId}");
            RaiseNetworkError(ConvertCode(disconnectInfo.Reason), disconnectInfo.SocketErrorCode);
        }
    }

    protected virtual void ReceiveData(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        var peerId = (uint)peer.Id;
        byte[] receivedBytes = new byte[reader.AvailableBytes];
        reader.GetBytes(receivedBytes, 0, receivedBytes.Length);
        NetPacket receivedPacket = new NetPacket(receivedBytes);
        RaiseNetworkReceived(peerId, receivedPacket, ConvertProtocolBack(deliveryMethod));
        reader.Recycle();
    }

    protected virtual void ReceiveDataUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        byte[] receivedBytes = new byte[reader.AvailableBytes];
        reader.GetBytes(receivedBytes, 0, receivedBytes.Length);
        NetPacket receivedPacket = new NetPacket(receivedBytes);
        RaiseNetworkReceivedUnconnected(remoteEndPoint, receivedPacket);
        reader.Recycle();
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        ConnectPeer(peer);
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        DisconnectPeer(peer, disconnectInfo);
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Debug.LogError($"<color=red><b>CNS</b></color>: Network error at {endPoint}: {socketError}");
        RaiseNetworkError(TransportCode.SocketError, socketError);
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        ReceiveData(peer, reader, deliveryMethod);
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        ReceiveDataUnconnected(remoteEndPoint, reader, messageType);
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Ignore
    }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        ConnectionRequested(request);
    }

    private static int SecondsToMilliseconds(float seconds)
    {
        return (int)Mathf.Ceil(seconds * 1000);
    }
}
#endif
