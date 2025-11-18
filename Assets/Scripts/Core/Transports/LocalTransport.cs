#if CNS_TRANSPORT_LOCAL
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class LocalTransport : NetTransport
{
    private static LocalTransport[] instances = new LocalTransport[2];
    private int instanceIndex = -1;

    private Queue<(uint remoteId, byte[] data, TransportMethod method)> queuedPackets = new Queue<(uint remoteId, byte[] data, TransportMethod method)>();
    private bool isConnecting = false;
    private bool isDisconnecting = false;

    public override uint ServerClientId => 0;

    void FixedUpdate()
    {
        PollEvents();
    }

    public override void Initialize(NetDeviceType deviceType)
    {
        this.deviceType = deviceType;
    }

    void PollEvents()
    {
        if (isConnecting)
        {
            isConnecting = false;
            RaiseNetworkConnected(ServerClientId);
        }

        if (isDisconnecting)
        {
            isDisconnecting = false;
            queuedPackets.Clear();
            instances[instanceIndex] = null;
            RaiseNetworkDisconnected(ServerClientId, TransportCode.ConnectionClosed);
        }

        while (queuedPackets.Count > 0)
        {
            var (remoteId, data, method) = queuedPackets.Dequeue();
            NetPacket receivedPacket = new NetPacket(data);
            RaiseNetworkReceived(remoteId, receivedPacket, method);
        }
    }

    protected override bool StartClient()
    {
        if (initialized)
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Already started as " + deviceType);
            return false;
        }

        if (!UpdateInstances())
        {
            return false;
        }

        initialized = true;

        if (instances[0] != null && instances[1] != null && instances[0].initialized && instances[1].initialized)
        {
            instances[instanceIndex].isConnecting = true;
            instances[1 - instanceIndex].isConnecting = true;
        }
        return true;
    }

    protected override bool StartServer()
    {
        if (initialized)
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Already started as " + deviceType);
            return false;
        }

        if (!UpdateInstances())
        {
            return false;
        }

        initialized = true;

        if (instances[0] != null && instances[1] != null && instances[0].initialized && instances[1].initialized)
        {
            instances[instanceIndex].isConnecting = true;
            instances[1 - instanceIndex].isConnecting = true;
        }
        return true;
    }

    public override void Send(uint remoteId, NetPacket packet, TransportMethod method)
    {
        var otherInstance = instances[1 - instanceIndex];
        if (otherInstance == null)
        {
            return;
        }

        otherInstance.queuedPackets.Enqueue((remoteId, packet.ByteArray, method));
    }

    public override void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod method)
    {
        Send(ServerClientId, packet, method);
    }

    public override void SendToAll(NetPacket packet, TransportMethod method)
    {
        Send(ServerClientId, packet, method);
    }

    public override void SendUnconnected(IPEndPoint ipEndPoint, NetPacket packet)
    {
        Debug.LogWarning("<color=yellow><b>CNS</b></color>: SendUnconnected is not supported by LocalTransport.");
    }

    public override void SendToListUnconnected(List<IPEndPoint> ipEndPoints, NetPacket packet)
    {
        Debug.LogWarning("<color=yellow><b>CNS</b></color>: SendToListUnconnected is not supported by LocalTransport.");
    }

    public override void BroadcastUnconnected(NetPacket packet)
    {
        Debug.LogWarning("<color=yellow><b>CNS</b></color>: BroadcastUnconnected is not supported by LocalTransport.");
    }

    public override void Disconnect()
    {
        isDisconnecting = true;
        if (instances[1 - instanceIndex] != null)
        {
            instances[1 - instanceIndex].isDisconnecting = true;
        }
    }

    public override void DisconnectRemote(uint remoteId)
    {
        Disconnect();
    }

    public override void Shutdown()
    {
        Disconnect();
        initialized = false;
    }

    private bool UpdateInstances()
    {
        if (instances[0] == null)
        {
            instances[0] = this;
            instanceIndex = 0;
        }
        else if (instances[1] == null)
        {
            instances[1] = this;
            instanceIndex = 1;
        }
        else
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: More than 2 instances of LocalTransport detected. Destroying extra instance.");
            Destroy(this);
            return false;
        }
        return true;
    }
}
#endif
