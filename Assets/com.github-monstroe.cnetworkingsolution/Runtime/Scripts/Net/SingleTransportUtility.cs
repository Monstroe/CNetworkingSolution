using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class SingleTransportUtility : MonoBehaviour, ITransportUtility
{
    public delegate void OnConnectedEventHandler(ulong remoteId);
    public event OnConnectedEventHandler OnSingleConnected;

    public delegate void OnDisconnectedEventHandler(ulong remoteId, TransportCode code);
    public event OnDisconnectedEventHandler OnSingleDisconnected;

    public delegate void OnReceivedEventHandler(ulong remoteId, NetPacket packet, TransportMethod? method);
    public event OnReceivedEventHandler OnSingleReceived;

    public delegate void OnReceivedUnconnectedEventHandler(IPEndPoint iPEndPoint, NetPacket packet);
    public event OnReceivedUnconnectedEventHandler OnSingleReceivedUnconnected;

    public delegate void OnErrorEventHandler(TransportCode code, SocketError? socketError);
    public event OnErrorEventHandler OnSingleError;

    public NetTransport Transport { get; set; }
    public List<ulong> ConnectedUserIds { get; set; } = new List<ulong>();

    public void SendToRemote(ulong remoteId, NetPacket packet, TransportMethod method)
    {
        Transport.Send((uint)remoteId, packet, method);
    }

    public void SendToRemotes(List<ulong> remoteIds, NetPacket packet, TransportMethod method)
    {
        Transport.SendToList(remoteIds.ConvertAll(id => (uint)id), packet, method);
    }

    public void SendToAllRemotes(NetPacket packet, TransportMethod method)
    {
        Transport.SendToAll(packet, method);
    }

    public void SendToUnconnectedRemote(IPEndPoint iPEndPoint, NetPacket packet)
    {
        Transport.SendUnconnected(iPEndPoint, packet);
    }

    public void SendToUnconnectedRemotes(List<IPEndPoint> iPEndPoints, NetPacket packet)
    {
        Transport.SendToListUnconnected(iPEndPoints, packet);
    }

    public void BroadcastToUnconnectedRemotes(NetPacket packet)
    {
        Transport.BroadcastUnconnected(packet);
    }

    public void KickRemote(ulong remoteId)
    {
        if (ConnectedUserIds.Remove(remoteId))
        {
            Transport.DisconnectRemote((uint)remoteId);
        }
    }

#nullable enable
    public void RegisterTransport(TransportType transportType, NetDeviceType deviceType, TransportSettings? transportSettings = null)
    {
        if (Transport != null)
        {
            RemoveTransports();
        }

        Transport = Instantiate(NetResources.Instance.TransportPrefabs[transportType], this.transform).GetComponent<NetTransport>();
        Transport.TransportData.TransportType = transportType;
        AddTransportEvents();
        Transport.Initialize(deviceType);
        Transport.StartDevice(transportSettings);
    }
#nullable disable

    public void AddTransport(NetTransport newTransport)
    {
        if (Transport != null)
        {
            RemoveTransports();
        }

        Transport = newTransport;
        Transport.transform.SetParent(this.transform);
        AddTransportEvents();
    }

    private void AddTransportEvents()
    {
        Transport.OnNetworkConnected += HandleNetworkConnected;
        Transport.OnNetworkDisconnected += HandleNetworkDisconnected;
        Transport.OnNetworkReceived += HandleNetworkReceived;
        Transport.OnNetworkReceivedUnconnected += HandleNetworkReceivedUnconnected;
        Transport.OnNetworkError += HandleNetworkError;
    }

    public void ClearTransportEvents()
    {
        Transport.OnNetworkConnected -= HandleNetworkConnected;
        Transport.OnNetworkDisconnected -= HandleNetworkDisconnected;
        Transport.OnNetworkReceived -= HandleNetworkReceived;
        Transport.OnNetworkReceivedUnconnected -= HandleNetworkReceivedUnconnected;
        Transport.OnNetworkError -= HandleNetworkError;
    }

    public void DisconnectTransports()
    {
        if (Transport != null)
        {
            ClearUserIds();
            Transport.Disconnect();
        }
    }

    public void RemoveTransport(NetTransport transport)
    {
        if (Transport == transport)
        {
            RemoveTransports();
        }
    }

    public void RemoveTransports()
    {
        if (Transport != null)
        {
            ClearUserIds();
            ClearTransportEvents();
            Transport.Shutdown();
            Destroy(Transport.gameObject);
            Transport = null;
        }
    }

    private void ClearUserIds()
    {
        ConnectedUserIds.Clear();
    }

    private void HandleNetworkConnected(NetTransport Transport, ConnectedArgs args)
    {
        ulong userId = args.RemoteId;
        if (!ConnectedUserIds.Contains(userId))
        {
            ConnectedUserIds.Add(userId);
            OnSingleConnected?.Invoke(userId);
        }
    }

    private void HandleNetworkDisconnected(NetTransport Transport, DisconnectedArgs args)
    {
        ulong userId = args.RemoteId;
        ConnectedUserIds.Remove(userId);
        OnSingleDisconnected?.Invoke(userId, args.Code);
    }

    private void HandleNetworkReceived(NetTransport Transport, ReceivedArgs args)
    {
        ulong userId = args.RemoteId;
        if (ConnectedUserIds.Contains(userId))
        {
            OnSingleReceived?.Invoke(userId, args.Packet, args.TransportMethod);
        }
    }

    private void HandleNetworkReceivedUnconnected(NetTransport Transport, ReceivedUnconnectedArgs args)
    {
        OnSingleReceivedUnconnected?.Invoke(args.IPEndPoint, args.Packet);
    }

    private void HandleNetworkError(NetTransport Transport, ErrorArgs args)
    {
        OnSingleError?.Invoke(args.Code, args.SocketError);
    }
}
