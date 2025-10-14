#if CNS_TRANSPORT_CNET && CNS_TRANSPORT_CNETRELAY && CNS_SYNC_HOST
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
    public override void Initialize(NetDeviceType deviceType)
    {
        base.Initialize(deviceType);

#if CNS_SERVER_MULTIPLE
        // Even though this transport only runs on the server (host), CLientManager should still exist in the scene
        if (ClientManager.Instance != null)
        {
            address = ClientManager.Instance.CurrentServerSettings.ServerAddress;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientManager instance not found in scene. Using default address {address}.");
        }
#endif
    }

    protected override bool StartServer()
    {
        return base.StartClient(); // Relay transport acts as a client to the relay server
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
        packet.Write((byte)RelayMessageType.DisconnectUser);
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

            if (ClientManager.Instance != null)
            {
#if CNS_SERVER_MULTIPLE
                base.SendToAll(PacketBuilder.ConnectionRequest(ClientManager.Instance.ConnectionToken), TransportMethod.Reliable);
#elif CNS_SERVER_SINGLE
                base.SendToAll(PacketBuilder.ConnectionRequest(ClientManager.Instance.ConnectionData), TransportMethod.Reliable);
#endif
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientManager instance not found in scene. Cannot send connection request to relay server.");
            }
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

            if (ClientManager.Instance != null && ServerManager.Instance != null)
            {
                ServerManager.Instance.KickUser(ClientManager.Instance.CurrentUser);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientManager or ServerManager instance not found in scene. Cannot kick host user on relay disconnect.");
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

        RelayMessageType receiveType = (RelayMessageType)packet.ReadByte();
        uint remoteId = packet.ReadUInt();

        switch (receiveType)
        {
            case RelayMessageType.ConnectUser:
                {
                    RaiseNetworkConnected(remoteId);
                    break;
                }
            case RelayMessageType.DisconnectUser:
                {
                    RaiseNetworkDisconnected(remoteId);
                    break;
                }
            case RelayMessageType.Data:
                {
                    byte[] data = new byte[packet.Length];
                    Buffer.BlockCopy(packet.ByteSegment.Array, packet.ByteSegment.Offset, data, 0, packet.Length);
                    NetPacket receivedPacket = new NetPacket(data);
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

    enum RelayMessageType
    {
        ConnectUser,
        DisconnectUser,
        Data
    }
}
#endif
