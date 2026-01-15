#if CNS_TRANSPORT_LITENETLIB && CNS_TRANSPORT_LITENETLIBBROADCAST
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using LiteNetLib;

public class LiteNetLibBroadcastTransport : LiteNetLibTransport
{
#nullable enable
    protected override bool StartClient(TransportSettings? transportSettings = null)
    {
        return StartServer(transportSettings);
    }

    protected override bool StartServer(TransportSettings? transportSettings = null)
    {
        return base.StartServer(transportSettings);
    }
#nullable disable

    public override void Send(uint remoteId, NetPacket packet, TransportMethod protocol)
    {
        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: LiteNetLibBroadcastTransport does not support sending to specific remote IDs. Broadcasting to all instead.");
    }

    public override void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod protocol)
    {
        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: LiteNetLibBroadcastTransport does not support sending to specific remote IDs. Broadcasting to all instead.");
    }

    public override void SendToAll(NetPacket packet, TransportMethod protocol)
    {
        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: LiteNetLibBroadcastTransport sending packet to all peers.");
    }

    protected override void ConnectionRequested(ConnectionRequest request)
    {
        // In broadcast mode, we don't accept connection requests.
    }

    protected override void ConnectPeer(NetPeer peer)
    {
        // In broadcast mode, we don't maintain connections to peers.
    }

    protected override void DisconnectPeer(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        // In broadcast mode, we don't maintain connections to peers.
    }

    protected override void ReceiveData(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: LiteNetLibBroadcastTransport does not support receiving data from connected peers.");
    }

    protected override void ReceiveDataUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        byte[] receivedBytes = new byte[reader.AvailableBytes];
        reader.GetBytes(receivedBytes, 0, receivedBytes.Length);
        NetPacket receivedPacket = new NetPacket(receivedBytes);
        RaiseNetworkReceivedUnconnected(remoteEndPoint, receivedPacket);
        reader.Recycle();
    }
}
#endif
