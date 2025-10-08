using System.Collections.Generic;
using UnityEngine;

public class CNetRelayTransport : CNetTransport
{


    public override void Send(uint remoteId, NetPacket packet, TransportMethod protocol)
    {
        packet.Insert(0, (byte)SendType.Single);
        packet.Insert(0, remoteId);
        base.SendToAll(packet, protocol);
    }

    public override void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod protocol)
    {
        packet.Insert(0, (byte)SendType.List);
        packet.Insert(0, (ushort)remoteIds.Count);
        foreach (var id in remoteIds)
        {
            packet.Insert(0, id);
        }
        base.SendToAll(packet, protocol);
    }

    public override void SendToAll(NetPacket packet, TransportMethod protocol)
    {
        packet.Insert(0, (byte)SendType.All);
        base.SendToAll(packet, protocol);
    }

    public enum SendType
    {
        Single,
        List,
        All
    }
}
