using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class SingleTransportUtility : MonoBehaviour
{
    public delegate void OnConnectedEventHandler(uint remoteId);
    public event OnConnectedEventHandler OnSingleConnected;

    public delegate void OnDisconnectedEventHandler(uint remoteId, TransportCode code);
    public event OnDisconnectedEventHandler OnSingleDisconnected;

    public delegate void OnReceivedEventHandler(uint remoteId, NetPacket packet, TransportMethod? method);
    public event OnReceivedEventHandler OnSingleReceived;

    public delegate void OnReceivedUnconnectedEventHandler(IPEndPoint iPEndPoint, NetPacket packet);
    public event OnReceivedUnconnectedEventHandler OnSingleReceivedUnconnected;

    public delegate void OnErrorEventHandler(TransportCode code, SocketError? socketError);
    public event OnErrorEventHandler OnSingleError;

    public NetTransport Transport { get; set; }

    public void SendToRemote(uint remoteId, NetPacket packet, TransportMethod method)
    {
        Transport.Send(remoteId, packet, method);
    }

    public void SendToRemotes(List<uint> remoteIds, NetPacket packet, TransportMethod method)
    {
        Transport.SendToList(remoteIds, packet, method);
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

    public void KickRemote(uint remoteId)
    {
        Transport.DisconnectRemote(remoteId);
    }

    public void RegisterTransport(TransportType transportType, NetDeviceType deviceType)
    {
        if (Transport != null)
        {
            RemoveTransport();
        }

        Transport = Instantiate(NetResources.Instance.TransportPrefabs[transportType], this.transform).GetComponent<NetTransport>();
        AddTransportEvents();
        Transport.Initialize(deviceType);
        Transport.StartDevice();
    }

    public void SetTransport(NetTransport newTransport)
    {
        if (Transport != null)
        {
            RemoveTransport();
        }

        Transport = newTransport;
        Transport.transform.SetParent(this.transform);
        AddTransportEvents();
    }

#if CNS_SYNC_HOST && CNS_LOBBY_MULTIPLE
    public void BridgeTransport()
    {
        if (Transport == null)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Attempted to bridge a null Transport.");
            return;
        }

        if (ServerManager.Instance == null)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Attempted to bridge Transport but ServerManager instance is null.");
            return;
        }

        ClearTransportEvents();
        ServerManager.Instance.AddTransport(Transport);
        Transport = null;
    }
#endif

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

    public void RemoveTransport()
    {
        if (Transport != null)
        {
            ClearTransportEvents();
            Transport.Shutdown();
            Destroy(Transport.gameObject);
            Transport = null;
        }
    }

    private void HandleNetworkConnected(NetTransport Transport, ConnectedArgs args)
    {
        OnSingleConnected?.Invoke(args.RemoteId);
    }

    private void HandleNetworkDisconnected(NetTransport Transport, DisconnectedArgs args)
    {
        OnSingleDisconnected?.Invoke(args.RemoteId, args.Code);
    }

    private void HandleNetworkReceived(NetTransport Transport, ReceivedArgs args)
    {
        OnSingleReceived?.Invoke(args.RemoteId, args.Packet, args.TransportMethod);
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
