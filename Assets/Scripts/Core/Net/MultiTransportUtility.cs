using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class MultiTransportUtility : MonoBehaviour
{
    public delegate void OnConnectedEventHandler(ulong remoteId);
    public event OnConnectedEventHandler OnMultiConnected;

    public delegate void OnDisconnectedEventHandler(ulong remoteId, TransportCode code);
    public event OnDisconnectedEventHandler OnMultiDisconnected;

    public delegate void OnReceivedEventHandler(ulong remoteId, NetPacket packet, TransportMethod? method);
    public event OnReceivedEventHandler OnMultiReceived;

    public delegate void OnReceivedUnconnectedEventHandler(IPEndPoint iPEndPoint, NetPacket packet);
    public event OnReceivedUnconnectedEventHandler OnMultiReceivedUnconnected;

    public delegate void OnErrorEventHandler(TransportCode code, SocketError? socketError);
    public event OnErrorEventHandler OnMultiError;

    public List<NetTransport> Transports { get; set; } = new List<NetTransport>();

    public void SendToRemote(ulong remoteId, NetPacket packet, TransportMethod method)
    {
        var (internalId, transportIndex) = GetRemoteIdAndTransportIndex(remoteId);
        Transports[transportIndex].Send(internalId, packet, method);
    }

    public void SendToRemotes(List<ulong> remoteIds, NetPacket packet, TransportMethod method)
    {
        Dictionary<NetTransport, List<uint>> userDict = new Dictionary<NetTransport, List<uint>>();
        foreach (var remoteId in remoteIds)
        {
            var (internalId, transportIndex) = GetRemoteIdAndTransportIndex(remoteId);
            if (!userDict.ContainsKey(Transports[transportIndex]))
            {
                userDict[Transports[transportIndex]] = new List<uint>();
            }
            userDict[Transports[transportIndex]].Add(internalId);
        }

        foreach (var (transport, internalIds) in userDict)
        {
            transport.SendToList(internalIds, packet, method);
        }
    }

    public void SendToAllRemotes(NetPacket packet, TransportMethod method)
    {
        foreach (NetTransport transport in Transports)
        {
            transport.SendToAll(packet, method);
        }
    }

    public void SendToUnconnectedRemote(IPEndPoint iPEndPoint, NetPacket packet)
    {
        foreach (NetTransport transport in Transports)
        {
            transport.SendUnconnected(iPEndPoint, packet);
        }
    }

    public void SendToUnconnectedRemotes(List<IPEndPoint> iPEndPoints, NetPacket packet)
    {
        foreach (NetTransport transport in Transports)
        {
            transport.SendToListUnconnected(iPEndPoints, packet);
        }
    }

    public void BroadcastToUnconnectedRemotes(NetPacket packet)
    {
        foreach (NetTransport transport in Transports)
        {
            transport.BroadcastUnconnected(packet);
        }
    }

    public void KickRemote(ulong remoteId)
    {
        var (internalId, transportIndex) = GetRemoteIdAndTransportIndex(remoteId);
        Transports[transportIndex].DisconnectRemote(internalId);
    }

    public void RegisterTransport(TransportType transportType, NetDeviceType deviceType)
    {
        NetTransport transport = Instantiate(NetResources.Instance.TransportPrefabs[transportType], this.transform).GetComponent<NetTransport>();
        Transports.Add(transport);
        AddTransportEvents(transport);
        transport.Initialize(deviceType);
        transport.StartDevice();
    }

    public void AddTransport(NetTransport transport)
    {
        Transports.Add(transport);
        transport.transform.SetParent(this.transform);
        AddTransportEvents(transport);
    }

    public void AddTransportEvents(NetTransport transport)
    {
        transport.OnNetworkConnected += HandleNetworkConnected;
        transport.OnNetworkDisconnected += HandleNetworkDisconnected;
        transport.OnNetworkReceived += HandleNetworkReceived;
        transport.OnNetworkReceivedUnconnected += HandleNetworkReceivedUnconnected;
        transport.OnNetworkError += HandleNetworkError;
    }

    public void ClearTransportEvents(NetTransport transport)
    {
        transport.OnNetworkConnected -= HandleNetworkConnected;
        transport.OnNetworkDisconnected -= HandleNetworkDisconnected;
        transport.OnNetworkReceived -= HandleNetworkReceived;
        transport.OnNetworkReceivedUnconnected -= HandleNetworkReceivedUnconnected;
        transport.OnNetworkError -= HandleNetworkError;
    }

    public void RemoveTransports()
    {
        foreach (NetTransport transport in Transports)
        {
            ClearTransportEvents(transport);
            transport.Shutdown();
            Destroy(transport.gameObject);
        }
        Transports.Clear();
    }

    private (uint, int) GetRemoteIdAndTransportIndex(ulong userId)
    {
        return ((uint)userId, (int)(userId >> 32));
    }

    private ulong CreateCombinedId(uint userId, NetTransport transport)
    {
        return (ulong)Transports.IndexOf(transport) << 32 | userId;
    }

    private void HandleNetworkConnected(NetTransport transport, ConnectedArgs args)
    {
        OnMultiConnected?.Invoke(CreateCombinedId(args.RemoteId, transport));
    }

    private void HandleNetworkDisconnected(NetTransport transport, DisconnectedArgs args)
    {
        OnMultiDisconnected?.Invoke(CreateCombinedId(args.RemoteId, transport), args.Code);
    }

    private void HandleNetworkReceived(NetTransport transport, ReceivedArgs args)
    {
        OnMultiReceived?.Invoke(CreateCombinedId(args.RemoteId, transport), args.Packet, args.TransportMethod);
    }

    private void HandleNetworkReceivedUnconnected(NetTransport transport, ReceivedUnconnectedArgs args)
    {
        OnMultiReceivedUnconnected?.Invoke(args.IPEndPoint, args.Packet);
    }

    private void HandleNetworkError(NetTransport transport, ErrorArgs args)
    {
        OnMultiError?.Invoke(args.Code, args.SocketError);
    }
}
