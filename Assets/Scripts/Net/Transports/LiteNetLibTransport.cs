#if CNS_TRANSPORT_LITENETLIB
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class LiteNetLibTransport : NetTransport, INetEventListener
{
    [Tooltip("The port to listen on (if server) or connect to (if client)")]
    [SerializeField] private ushort port = 7777;
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
    [Tooltip("Size of default buffer for decoding incoming packets, in bytes")]
    [SerializeField] private int messageBufferSize = 1024 * 5;
    [Tooltip("Simulated chance for a packet to be \"lost\", from 0 (no simulation) to 100 percent")]
    [SerializeField] private int simulatePacketLossChance = 0;
    [Tooltip("Simulated minimum additional latency for packets in milliseconds (0 for no simulation)")]
    [SerializeField] private int simulateMinLatency = 0;
    [Tooltip("Simulated maximum additional latency for packets in milliseconds (0 for no simulation")]
    [SerializeField] private int simulateMaxLatency = 0;

    private NetManager netManager;
    private readonly Dictionary<uint, NetPeer> connectedPeers = new Dictionary<uint, NetPeer>();

    public override uint ServerClientId => 0;
    public override List<uint> ConnectedClientIds => new List<uint>(connectedPeers.Keys);

    void OnValidate()
    {
        pingInterval = Math.Max(0, pingInterval);
        disconnectTimeout = Math.Max(0, disconnectTimeout);
        reconnectDelay = Math.Max(0, reconnectDelay);
        maxConnectAttempts = Math.Max(0, maxConnectAttempts);
        messageBufferSize = Math.Max(0, messageBufferSize);
        simulatePacketLossChance = Math.Min(100, Math.Max(0, simulatePacketLossChance));
        simulateMinLatency = Math.Max(0, simulateMinLatency);
        simulateMaxLatency = Math.Max(simulateMinLatency, simulateMaxLatency);
    }

    void OnDestroy()
    {
        if (hostType != NetDeviceType.None)
        {
            Shutdown();
        }
    }

    void FixedUpdate()
    {
        netManager?.PollEvents();
    }

    public override void Initialize()
    {
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
            SimulationMaxLatency = simulateMaxLatency
        };

#nullable enable
        if (ClientManager.Instance)
        {
            ClientManager.Instance.OnLobbyCreateRequested += (lobbyId, lobbySettings, serverSettings, gameServerToken) =>
            {
                address = serverSettings?.ServerAddress ?? address;
                StartClient();
            };

            ClientManager.Instance.OnLobbyJoinRequested += (lobbyId, lobbySettings, serverSettings, gameServerToken) =>
            {
                address = serverSettings?.ServerAddress ?? address;
                StartClient();
            };
        }
#nullable disable
    }

    public override bool StartClient()
    {
        if (hostType != NetDeviceType.None)
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Already started as " + hostType);
            return false;
        }

        hostType = NetDeviceType.Client;

        var success = netManager.Start();
        if (!success)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Failed to start LiteNetLib transport.");
            return false;
        }

        NetPeer peer = netManager.Connect(address, port, connectionKey);

        if (peer.Id != (int)ServerClientId)
        {
            throw new InvalidPacketException("<color=red><b>CNS</b></color>: Server peer did not have id 0: " + peer.Id);
        }

        return true;
    }

    public override bool StartServer()
    {
        if (hostType != NetDeviceType.None)
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Already started as " + hostType);
            return false;
        }

        hostType = NetDeviceType.Server;

        bool success = netManager.Start(port);
        if (!success)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Failed to start LiteNetLib transport.");
            return false;
        }

        return true;
    }

    public override void Send(uint remoteId, NetPacket packet, TransportMethod protocol)
    {
        if (!connectedPeers.ContainsKey(remoteId))
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to send to a peer that is not connected: {remoteId}");
            return;
        }

        var deliveryMethod = ConvertProtocol(protocol);
        connectedPeers[remoteId].Send(packet.ByteArray, deliveryMethod);
    }

    public override void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod protocol)
    {
        var deliveryMethod = ConvertProtocol(protocol);

        foreach (var remoteId in remoteIds)
        {
            if (connectedPeers.TryGetValue(remoteId, out NetPeer peer))
            {
                peer.Send(packet.ByteArray, deliveryMethod);
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
        netManager.SendToAll(packet.ByteArray, deliveryMethod);
    }

    public override void Disconnect()
    {
        netManager.DisconnectAll();
    }

    public override void DisconnectRemote(uint remoteId)
    {
        if (connectedPeers.ContainsKey(remoteId))
        {
            connectedPeers[remoteId].Disconnect();
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to disconnect a peer that is not connected: {remoteId}");
        }
    }

    public override void Shutdown()
    {
        try
        {
            if (netManager != null)
            {
                Disconnect();
                netManager.Stop();
            }

            hostType = NetDeviceType.None;
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Exception occurred while shutting down: " + e.Message);
        }
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

    private TransportMethod ConvertProtocolBack(DeliveryMethod method)
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

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        var peerId = (uint)peer.Id;

        if (!connectedPeers.ContainsKey(peerId))
        {
            connectedPeers[peerId] = peer;
            RaiseNetworkConnected(peerId);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to connect a peer that is already connected: {peerId}");
        }
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        var peerId = (uint)peer.Id;

        if (connectedPeers.Remove(peerId))
        {
            RaiseNetworkDisconnected(peerId);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to disconnect a peer that is not connected: {peerId}");
        }
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Debug.LogError($"<color=red><b>CNS</b></color>: Network error at {endPoint}: {socketError}");
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var peerId = (uint)peer.Id;

        byte[] receivedBytes = new byte[reader.AvailableBytes];
        reader.GetBytes(receivedBytes, 0, receivedBytes.Length);
        NetPacket receivedPacket = new NetPacket(receivedBytes);

        RaiseNetworkReceived(peerId, receivedPacket, ConvertProtocolBack(deliveryMethod));

        reader.Recycle();
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Ignore
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Ignore
    }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey(connectionKey);
    }

    private static int SecondsToMilliseconds(float seconds)
    {
        return (int)Mathf.Ceil(seconds * 1000);
    }
}
#endif
