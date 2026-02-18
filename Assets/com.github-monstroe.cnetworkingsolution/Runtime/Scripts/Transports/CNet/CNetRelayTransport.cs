#if CNS_TRANSPORT_CNET && CNS_TRANSPORT_CNETRELAY && CNS_TRANSPORT_LOCAL && CNS_SYNC_HOST && CNS_LOBBY_MULTIPLE
using System;
using System.Buffers;
using System.Collections.Generic;
using CNet;
using UnityEngine;

public class CNetRelayTransport : CNetTransport
{
#nullable enable
    protected override bool StartServer(TransportSettings? transportSettings = null)
    {
        throw new NotImplementedException("<color=red><b>CNS</b></color>: CNetRelayTransport does not support starting a server directly. This transport is intended to be used by a client acting as a server (host).");
    }
#nullable disable

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
            TransportData.ConnectedClientIds.Add(remoteEPId);
            Debug.Log($"<color=green><b>CNS</b></color>: Connected to relay server: {remoteEP.TCPEndPoint}");

#if CNS_SERVER_MULTIPLE
            base.SendToAll(PacketBuilder.ConnectionRequest(ClientManager.Instance.WebAPI.ConnectionToken), TransportMethod.Reliable);
#elif CNS_SERVER_SINGLE
            base.SendToAll(PacketBuilder.ConnectionRequest(ClientManager.Instance.ConnectionData), TransportMethod.Reliable);
#endif
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to connect to relay server that is already connected: {remoteEP.TCPEndPoint}");
        }
    }

    // This function should only be called once when the client (acting as a server) disconnects from the relay server
    protected override void DisconnectRemoteEP(NetEndPoint remoteEP, NetDisconnect disconnect)
    {
        var remoteEPId = remoteEP.ID;

        if (connectedEPs.Remove(remoteEPId))
        {
            TransportData.ConnectedClientIds.Remove(remoteEPId);
            Debug.Log($"<color=green><b>CNS</b></color>: Disconnected from relay server: {remoteEP.TCPEndPoint}");
            if (ServerManager.Instance != null)
            {
                ServerManager.Instance.KickUser(ClientManager.Instance.CurrentLobby.CurrentUser);
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unknown endpoint disconnected: {remoteEP.TCPEndPoint}");
        }
    }

    protected override void ReceivePacket(NetEndPoint remoteEP, CNet.NetPacket packet, TransportProtocol protocol)
    {
        if (packet.Length < 5) // Minimum length: 1 byte for RelayMessageType + 4 bytes for userId
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received packet that is too short from relay server: {remoteEP.TCPEndPoint}");
            return;
        }

        NetPacket receivedPacket = new NetPacket(packet.ByteSegment);
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
                    TransportCode code = (TransportCode)receivedPacket.ReadByte();
                    RaiseNetworkDisconnected(remoteId, code);
                    break;
                }
            case RelayMessageType.Data:
                {
                    receivedPacket.Remove(0, 5); // Remove the first 5 bytes
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
