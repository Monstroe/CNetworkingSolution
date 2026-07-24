#if CNS_TRANSPORT_LITENETLIB && CNS_TRANSPORT_LITENETLIBBROADCAST
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using Monstroe.CNetworkingSolution;

public class LiteNetLibBroadcastTransport : LiteNetLibTransport
{
    protected override bool StartClient()
    {
        return base.StartServer();
    }

    protected override bool StartServer()
    {
        return base.StartServer();
    }

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
        // In broadcast mode, we don't receive connected data from peers.
    }
}
#endif
