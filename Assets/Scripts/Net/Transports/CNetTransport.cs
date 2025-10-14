#if CNS_TRANSPORT_CNET
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using CNet;
using UnityEngine;

public class CNetTransport : NetTransport, IEventNetListener, IEventNetClient
{
    [Header("Connection Settings")]
    [Tooltip("The port to listen on (if server) or connect to (if client)")]
    [SerializeField] private ushort port = 7777;
    [Tooltip("The address to connect to as client; ignored if server")]
    [SerializeField] protected string address = "127.0.0.1";
    [Tooltip("The key used to successfully connect to the server")]
    [SerializeField] private string connectionKey = "Bruh-Wizz-Arcgis";
    [Tooltip("The maximum number of pending connections the server can have")]
    [SerializeField] private int maxPendingConnections = 100;

    [Header("TCP Settings")]
    [Tooltip("The interval (in milliseconds) at which to send heartbeat packets to keep the connection alive")]
    [SerializeField] private int tcpHeartbeatInterval = 2500;
    [Tooltip("The timeout (in milliseconds) after which a connection is considered lost if no heartbeat packets are received")]
    [SerializeField] private int tcpConnectionTimeout = 5000;
    [Tooltip("The size of the socket receive buffer; 0 for default")]
    [SerializeField] private int tcpSocketReceiveBufferSize = 0;
    [Tooltip("The size of the socket send buffer; 0 for default")]
    [SerializeField] private int tcpSocketSendBufferSize = 0;
    [Tooltip("The maximum size of a single packet; larger packets will be fragmented")]
    [SerializeField] private int tcpMaxPacketSize = 1024;

    [Header("UDP Settings")]
    [Tooltip("The interval (in milliseconds) at which to send heartbeat packets to keep the connection alive")]
    [SerializeField] private int udpHeartbeatInterval = 500;
    [Tooltip("The timeout (in milliseconds) after which a connection is considered lost if no heartbeat packets are received")]
    [SerializeField] private int udpConnectionTimeout = 5000;
    [Tooltip("The size of the socket receive buffer; 0 for default")]
    [SerializeField] private int udpSocketReceiveBufferSize = 0;
    [Tooltip("The size of the socket send buffer; 0 for default")]
    [SerializeField] private int udpSocketSendBufferSize = 0;
    [Tooltip("The maximum size of a single packet; larger packets will be fragmented")]
    [SerializeField] private int udpMaxPacketSize = 1024;

    private NetSystem netSystem;
    protected readonly Dictionary<uint, NetEndPoint> connectedEPs = new Dictionary<uint, NetEndPoint>();

    public override uint ServerClientId => 0;
    public override List<uint> ConnectedClientIds => new List<uint>(connectedEPs.Keys);

    void FixedUpdate()
    {
        netSystem?.Update();
    }

    public override void Initialize(NetDeviceType deviceType)
    {
        this.deviceType = deviceType;

        netSystem = new NetSystem();
        netSystem.TCP.HEARTBEAT_INTERVAL = tcpHeartbeatInterval;
        netSystem.TCP.CONNECTION_TIMEOUT = tcpConnectionTimeout;
        netSystem.TCP.SOCKET_RECEIVE_BUFFER_SIZE = tcpSocketReceiveBufferSize;
        netSystem.TCP.SOCKET_SEND_BUFFER_SIZE = tcpSocketSendBufferSize;
        netSystem.TCP.MAX_PACKET_SIZE = tcpMaxPacketSize;

        netSystem.UDP.HEARTBEAT_INTERVAL = udpHeartbeatInterval;
        netSystem.UDP.CONNECTION_TIMEOUT = udpConnectionTimeout;
        netSystem.UDP.SOCKET_RECEIVE_BUFFER_SIZE = udpSocketReceiveBufferSize;
        netSystem.UDP.SOCKET_SEND_BUFFER_SIZE = udpSocketSendBufferSize;
        netSystem.UDP.MAX_PACKET_SIZE = udpMaxPacketSize;

        netSystem.MaxPendingConnections = maxPendingConnections;

        if (deviceType == NetDeviceType.Client)
        {
            ClientManager.Instance.OnLobbyCreateRequested += LobbyCreateRequested;
            ClientManager.Instance.OnLobbyJoinRequested += LobbyJoinRequested;
        }
    }

    private void LobbyCreateRequested(ServerSettings serverSettings)
    {
        address = serverSettings?.ServerAddress ?? address;
        StartClient();
    }

    private void LobbyJoinRequested(int lobbyId, ServerSettings serverSettings)
    {
        address = serverSettings?.ServerAddress ?? address;
        StartClient();
    }

    protected override bool StartClient()
    {
        if (initialized)
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Already started as " + deviceType);
            return false;
        }
        initialized = true;
        netSystem.RegisterInterface((IEventNetClient)this);
        netSystem.Connect(address, port, connectionKey);
        Debug.Log($"<color=green><b>CNS</b></color>: Client connecting to {address}:{port}");
        return true;
    }

    protected override bool StartServer()
    {
        if (initialized)
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Already started as " + deviceType);
            return false;
        }
        initialized = true;
        netSystem.RegisterInterface((IEventNetListener)this);
        netSystem.Listen(port);
        Debug.Log($"<color=green><b>CNS</b></color>: Server listening on port {port}");
        return true;
    }

    public override void Send(uint remoteId, NetPacket packet, TransportMethod protocol)
    {
        var transportProtocol = ConvertProtocol(protocol);
        if (connectedEPs.TryGetValue(remoteId, out NetEndPoint remoteEP))
        {
            SendInternal(remoteEP, packet.ByteArray, transportProtocol);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to send to an endpoint that is not connected: {remoteId}");
        }
    }

    public override void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod protocol)
    {
        var transportProtocol = ConvertProtocol(protocol);
        foreach (var remoteId in remoteIds)
        {
            if (connectedEPs.TryGetValue(remoteId, out NetEndPoint remoteEP))
            {
                SendInternal(remoteEP, packet.ByteArray, transportProtocol);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to send to an endpoint that is not connected: {remoteId}");
            }
        }
    }

    public override void SendToAll(NetPacket packet, TransportMethod protocol)
    {
        var transportProtocol = ConvertProtocol(protocol);
        foreach (var remoteEP in connectedEPs.Values)
        {
            SendInternal(remoteEP, packet.ByteArray, transportProtocol);
        }
    }

    private void SendInternal(NetEndPoint remoteEP, ArraySegment<byte> data, TransportProtocol protocol)
    {
        using (CNet.NetPacket packet = new CNet.NetPacket(netSystem, protocol, data))
        {
            remoteEP.Send(packet, protocol);
        }
    }

    public override void Disconnect()
    {
        netSystem.Close(true);
    }

    public override void DisconnectRemote(uint remoteId)
    {
        if (connectedEPs.TryGetValue(remoteId, out NetEndPoint remoteEP))
        {
            remoteEP.Disconnect();
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to disconnect an endpoint that is not connected: {remoteId}");
        }
    }

    public override void Shutdown()
    {
        if (netSystem != null)
        {
            netSystem.Dispose();
        }

        if (deviceType == NetDeviceType.Client)
        {
            ClientManager.Instance.OnLobbyCreateRequested -= LobbyCreateRequested;
            ClientManager.Instance.OnLobbyJoinRequested -= LobbyJoinRequested;
        }

        initialized = false;
    }

    private TransportProtocol ConvertProtocol(TransportMethod protocol)
    {
        switch (protocol)
        {
            case TransportMethod.Unreliable:
                {
                    return TransportProtocol.UDP;
                }
            case TransportMethod.UnreliableSequenced:
                {
                    Debug.LogWarning("<color=yellow><b>CNS</b></color>: UnreliableSequenced is not supported by CNet. Falling back to Reliable.");
                    return TransportProtocol.TCP;
                }
            case TransportMethod.Reliable:
                {
                    return TransportProtocol.TCP;
                }
            case TransportMethod.ReliableUnordered:
                {
                    Debug.LogWarning("<color=yellow><b>CNS</b></color>: ReliableUnordered is not supported by CNet. Falling back to Reliable.");
                    return TransportProtocol.TCP;
                }
            default:
                {
                    throw new ArgumentOutOfRangeException("<color=red><b>CNS</b></color>: Unknown protocol: " + protocol);
                }
        }
    }

    protected TransportMethod ConvertProtocolBack(TransportProtocol method)
    {
        switch (method)
        {
            case TransportProtocol.UDP:
                {
                    return TransportMethod.Unreliable;
                }
            case TransportProtocol.TCP:
                {
                    return TransportMethod.Reliable;
                }
            default:
                {
                    throw new ArgumentOutOfRangeException("<color=red><b>CNS</b></color>: Unknown protocol: " + method);
                }
        }
    }

    protected virtual void ConnectRemoteEP(NetEndPoint remoteEP)
    {
        var remoteEPId = remoteEP.ID;

        if (!connectedEPs.ContainsKey(remoteEPId))
        {
            connectedEPs[remoteEPId] = remoteEP;
            RaiseNetworkConnected(remoteEPId);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to connect an endpoint that is already connected: {remoteEPId}");
        }
    }

    protected virtual void DisconnectRemoteEP(NetEndPoint remoteEP, NetDisconnect disconnect)
    {
        var remoteEPId = remoteEP.ID;

        if (connectedEPs.Remove(remoteEPId))
        {
            RaiseNetworkDisconnected(remoteEPId);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to disconnect an endpoint that is not connected: {remoteEPId}");
        }
    }

    protected virtual void ReceivePacket(NetEndPoint remoteEP, CNet.NetPacket packet, TransportProtocol protocol)
    {
        byte[] data = new byte[packet.Length];
        Buffer.BlockCopy(packet.ByteSegment.Array, packet.ByteSegment.Offset, data, 0, packet.Length);
        NetPacket receivedPacket = new NetPacket(data);
        RaiseNetworkReceived(remoteEP.ID, receivedPacket, ConvertProtocolBack(protocol));
    }

    void IEventNetClient.OnConnected(NetEndPoint remoteEP)
    {
        Debug.Log($"<color=green><b>CNS</b></color>: Connected to {remoteEP}");
        ConnectRemoteEP(remoteEP);
    }

    void IEventNetClient.OnDisconnected(NetEndPoint remoteEP, NetDisconnect disconnect)
    {
        DisconnectRemoteEP(remoteEP, disconnect);
    }

    void IEventNetClient.OnPacketReceived(NetEndPoint remoteEP, CNet.NetPacket packet, TransportProtocol protocol)
    {
        ReceivePacket(remoteEP, packet, protocol);
    }

    void IEventNetClient.OnNetworkError(NetEndPoint remoteEP, SocketError error)
    {
        Debug.LogError($"<color=red><b>CNS</b></color>: Network error from {remoteEP?.TCPEndPoint.ToString() ?? "unknown endpoint"}: {error}");
    }

    void IEventNetListener.OnClientConnected(NetEndPoint remoteEP)
    {
        ConnectRemoteEP(remoteEP);
    }

    void IEventNetListener.OnClientDisconnected(NetEndPoint remoteEP, NetDisconnect disconnect)
    {
        DisconnectRemoteEP(remoteEP, disconnect);
    }

    void IEventNetListener.OnConnectionRequest(NetRequest request)
    {
        request.AcceptIfKey(connectionKey);
    }

    void IEventNetListener.OnNetworkError(NetEndPoint remoteEP, SocketError error)
    {
        Debug.LogError($"<color=red><b>CNS</b></color>: Network error from {remoteEP?.ToString() ?? "unknown endpoint"}: {error}");
    }

    void IEventNetListener.OnPacketReceived(NetEndPoint remoteEP, CNet.NetPacket packet, TransportProtocol protocol)
    {
        ReceivePacket(remoteEP, packet, protocol);
    }
}
#endif
