using System;
using System.Collections.Generic;
using CNet;
using UnityEngine;

/// <summary>
/// This transport is designed to work with a relay server using the CNet library.
/// It handles sending and receiving packets through the relay server, allowing for communication between multiple clients.
/// The relay server is expected to forward packets to the appropriate clients based on the user IDs included in the packets.
/// ONLY USE THIS TRANSPORT IF THIS CLIENT IS ACTING AS A SERVER (HOSTING A LOBBY).
/// OTHERWISE, USE CNetTransport.
/// </summary>
public class CNetRelayTransport : CNetTransport
{
    protected override bool StartServer()
    {
        return base.StartClient(); // Relay transport acts as a client to the relay server
    }

    public override void Send(uint remoteId, NetPacket packet, TransportMethod protocol)
    {
        packet.Insert(0, (byte)SendType.Single);
        packet.Insert(0, remoteId);
        base.SendToAll(packet, protocol);
    }

    public override void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod protocol)
    {
        packet.Insert(0, (byte)SendType.List);
        packet.Insert(0, (byte)remoteIds.Count);
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

    // This function should only be called once when the client (acting as a server) connects to the relay server
    protected override void ConnectRemoteEP(NetEndPoint remoteEP)
    {
        var remoteEPId = remoteEP.ID;

        if (!connectedEPs.ContainsKey(remoteEPId))
        {
            connectedEPs[remoteEPId] = remoteEP;
            Debug.Log($"<color=green><b>CNS</b></color>: Connected to relay endpoint: {remoteEP.TCPEndPoint}");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to connect to relay endpoint that is already connected: {remoteEP.TCPEndPoint}");
        }
    }

    protected override void DisconnectRemoteEP(NetEndPoint remoteEP, NetDisconnect disconnect)
    {
        var remoteEPId = remoteEP.ID;

        if (connectedEPs.Remove(remoteEPId))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Disconnected from relay endpoint: {remoteEP.TCPEndPoint}");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to disconnect from relay endpoint that is not connected: {remoteEP.TCPEndPoint}");
        }
    }

    protected override void ReceivePacket(NetEndPoint remoteEP, CNet.NetPacket packet, TransportProtocol protocol)
    {
        if (packet.Length < 5) // Minimum length: 1 byte for ReceiveType + 4 bytes for userId
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received packet that is too short from relay endpoint: {remoteEP.TCPEndPoint}");
            return;
        }

        ReceiveType receiveType = (ReceiveType)packet.ReadByte();
        uint remoteEPId = packet.ReadUInt();

        switch (receiveType)
        {
            case ReceiveType.ConnectedUser:
                {
                    RaiseNetworkConnected(remoteEPId);
                    break;
                }
            case ReceiveType.DisconnectedUser:
                {
                    RaiseNetworkDisconnected(remoteEPId);
                    break;
                }
            case ReceiveType.Data:
                {
                    byte[] data = new byte[packet.Length];
                    Buffer.BlockCopy(packet.ByteSegment.Array, packet.ByteSegment.Offset, data, 0, packet.Length);
                    NetPacket receivedPacket = new NetPacket(data);
                    RaiseNetworkReceived(remoteEPId, receivedPacket, ConvertProtocolBack(protocol));
                    break;
                }
            default:
                {
                    Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received packet with unknown ReceiveType from userId: {remoteEPId}");
                    break;
                }
        }
    }

    public enum SendType
    {
        Single,
        List,
        All
    }

    public enum ReceiveType
    {
        ConnectedUser,
        DisconnectedUser,
        Data
    }
}
