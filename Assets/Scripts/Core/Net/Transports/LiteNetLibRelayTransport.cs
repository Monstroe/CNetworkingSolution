#if CNS_TRANSPORT_LITENETLIB && CNS_TRANSPORT_LITENETLIBRELAY && CNS_TRANSPORT_LOCAL && CNS_SYNC_HOST && CNS_LOBBY_MULTIPLE
using System;
using System.Collections.Generic;
using LiteNetLib;
using UnityEngine;

public class LiteNetLibRelayTransport : LiteNetLibTransport
{
    protected override bool StartServer()
    {
        throw new NotImplementedException("<color=red><b>CNS</b></color>: CNetRelayTransport does not support starting a server directly. This transport is intended to be used by a client acting as a server (host).");
    }

    public override void Send(uint remoteId, NetPacket packet, TransportMethod protocol)
    {
        packet.Insert(0, (byte)RelayMessageType.Data);
        packet.Insert(1, (byte)RelayUserSendType.Single);
        packet.Insert(2, remoteId);
        base.SendToAll(packet, protocol);
    }

    public override void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod protocol)
    {
        packet.Insert(0, (byte)RelayMessageType.Data);
        packet.Insert(1, (byte)RelayUserSendType.List);
        packet.Insert(2, (byte)remoteIds.Count);
        for (int i = 0; i < remoteIds.Count; i++)
        {
            packet.Insert(3 + i * 4, remoteIds[i]);
        }
        base.SendToAll(packet, protocol);
    }

    public override void SendToAll(NetPacket packet, TransportMethod protocol)
    {
        packet.Insert(0, (byte)RelayMessageType.Data);
        packet.Insert(1, (byte)RelayUserSendType.All);
        base.SendToAll(packet, protocol);
    }

    public override void DisconnectRemote(uint remoteId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)RelayMessageType.DisconnectedUser);
        packet.Write(remoteId);
        base.SendToAll(packet, TransportMethod.Reliable);
    }

    // This function should only be called once when the client (acting as a server) connects to the relay server
    protected override void ConnectPeer(NetPeer peer)
    {
        var peerId = (uint)peer.Id;

        if (!connectedPeers.ContainsKey(peerId))
        {
            connectedPeers[peerId] = peer;
            Debug.Log($"<color=green><b>CNS</b></color>: Connected to relay server: {peer.Address}");

#if CNS_SERVER_MULTIPLE
            base.SendToAll(PacketBuilder.ConnectionRequest(ClientManager.Instance.ConnectionToken), TransportMethod.Reliable);
#elif CNS_SERVER_SINGLE
            base.SendToAll(PacketBuilder.ConnectionRequest(ClientManager.Instance.ConnectionData), TransportMethod.Reliable);
#endif
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to connect relay server that is already connected: {peer.Address}");
        }
    }

    // This function should only be called once when the client (acting as a server) disconnects from the relay server
    protected override void DisconnectPeer(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (disconnectInfo.AdditionalData.AvailableBytes > 0)
        {
            ReceiveData(peer, disconnectInfo.AdditionalData, DeliveryMethod.ReliableOrdered);
        }

        var peerId = (uint)peer.Id;
        if (connectedPeers.Remove(peerId))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Disconnected from relay server: {peer.Address}");
            if (ServerManager.Instance != null)
            {
                ServerManager.Instance.KickUser(ClientManager.Instance.CurrentUser);
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to disconnect a peer that is not connected: {peerId}");
        }
    }

    protected override void ReceiveData(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        if (reader.AvailableBytes < 5) // Minimum length: 1 byte for RelayMessageType + 4 bytes for userId
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received packet that is too short from relay server: {peer.Address}");
            return;
        }

        byte[] receivedBytes = new byte[reader.AvailableBytes];
        reader.GetBytes(receivedBytes, 0, receivedBytes.Length);
        NetPacket receivedPacket = new NetPacket(receivedBytes);
        reader.Recycle();

        RelayMessageType receiveType = (RelayMessageType)receivedPacket.ReadByte();
        uint remoteId = receivedPacket.ReadUInt();

        switch (receiveType)
        {
            case RelayMessageType.ConnectionResponse:
                {
                    int lobbyId = receivedPacket.ReadInt();
                    Debug.Log($"<color=green><b>CNS</b></color>: Connection to relay server accepted: " + lobbyId);
                    ClientManager.Instance.ConnectionData.LobbyId = lobbyId;
                    Instantiate(NetResources.Instance.ServerPrefab);
                    ClientManager.Instance.BridgeTransport();
                    ServerManager.Instance.RegisterTransport(TransportType.Local);
                    ClientManager.Instance.RegisterTransport(TransportType.Local);
                    break;
                }
            case RelayMessageType.ConnectedUser:
                {
                    RaiseNetworkConnected(remoteId);
                    break;
                }
            case RelayMessageType.DisconnectedUser:
                {
                    RaiseNetworkDisconnected(remoteId);
                    break;
                }
            case RelayMessageType.Data:
                {
                    receivedPacket.Remove(0, 5); // Remove the first 5 bytes
                    RaiseNetworkReceived(remoteId, receivedPacket, ConvertProtocolBack(deliveryMethod));
                    break;
                }
            default:
                {
                    Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received packet with unknown RelayMessageType from userId: {remoteId}");
                    break;
                }
        }
    }





    enum RelayMessageType
    {
        ConnectionResponse,
        ConnectedUser,
        DisconnectedUser,
        Data
    }

    enum RelayUserSendType
    {
        Single,
        List,
        All
    }
}
#endif
