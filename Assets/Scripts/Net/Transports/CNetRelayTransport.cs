#if CNS_TRANSPORT_CNET && CNS_TRANSPORT_CNETRELAY && CNS_SYNC_HOST && CNS_LOBBY_MULTIPLE
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
    protected override void ConnectRemoteEP(NetEndPoint remoteEP)
    {
        var remoteEPId = remoteEP.ID;

        if (!connectedEPs.ContainsKey(remoteEPId))
        {
            connectedEPs[remoteEPId] = remoteEP;
            Debug.Log($"<color=green><b>CNS</b></color>: Connected to relay endpoint: {remoteEP.TCPEndPoint}");

#if CNS_SERVER_MULTIPLE
            base.SendToAll(PacketBuilder.ConnectionRequest(ClientManager.Instance.ConnectionToken), TransportMethod.Reliable);
#elif CNS_SERVER_SINGLE
            base.SendToAll(PacketBuilder.ConnectionRequest(ClientManager.Instance.ConnectionData), TransportMethod.Reliable);
#endif
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to connect to relay endpoint that is already connected: {remoteEP.TCPEndPoint}");
        }
    }

    // This function should only be called once when the client (acting as a server) disconnects from the relay server
    protected override void DisconnectRemoteEP(NetEndPoint remoteEP, NetDisconnect disconnect)
    {
        var remoteEPId = remoteEP.ID;

        if (connectedEPs.Remove(remoteEPId))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Disconnected from relay endpoint: {remoteEP.TCPEndPoint}");
            if (ServerManager.Instance != null)
            {
                ServerManager.Instance.KickUser(ClientManager.Instance.CurrentUser);
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to disconnect from relay endpoint that is not connected: {remoteEP.TCPEndPoint}");
        }
    }

    protected override void ReceivePacket(NetEndPoint remoteEP, CNet.NetPacket packet, TransportProtocol protocol)
    {
        if (packet.Length < 5) // Minimum length: 1 byte for RelayMessageType + 4 bytes for userId
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received packet that is too short from relay endpoint: {remoteEP.TCPEndPoint}");
            return;
        }

        byte[] data = new byte[packet.Length];
        Buffer.BlockCopy(packet.ByteSegment.Array, packet.ByteSegment.Offset, data, 0, packet.Length);
        NetPacket receivedPacket = new NetPacket(data);

        RelayMessageType receiveType = (RelayMessageType)receivedPacket.ReadByte();
        uint remoteId = receivedPacket.ReadUInt();

        switch (receiveType)
        {
            case RelayMessageType.ConnectionResponse:
                {
                    bool accepted = receivedPacket.ReadBool();
                    int lobbyId = receivedPacket.ReadInt();
                    if (accepted)
                    {
                        Debug.Log($"<color=green><b>CNS</b></color>: Connection to relay server accepted: " + lobbyId);
                        ClientManager.Instance.ConnectionData.LobbyId = lobbyId;
                        Instantiate(GameResources.Instance.ServerPrefab);
                        ServerManager.Instance.RegisterTransport(TransportType.Local);
                        ClientManager.Instance.BridgeTransport();
                        ClientManager.Instance.RegisterTransport(TransportType.Local);
                    }
                    else
                    {
                        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Connection to relay server rejected.");
                    }
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
                    RaiseNetworkReceived(remoteId, receivedPacket, ConvertProtocolBack(protocol));
                    break;
                }
            default:
                {
                    Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received packet with unknown RelayMessageType from userId: {remoteId}");
                    break;
                }
        }
    }

    enum RelayUserSendType
    {
        Single,
        List,
        All
    }

    public enum RelayMessageType
    {
        ConnectionResponse,
        ConnectedUser,
        DisconnectedUser,
        Data
    }
}
#endif
