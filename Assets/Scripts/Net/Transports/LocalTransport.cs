#if CNS_TRANSPORT_LOCAL
using System.Collections.Generic;
using UnityEngine;

public class LocalTransport : NetTransport
{
    private static LocalTransport[] instances = new LocalTransport[2];
    private int instanceIndex = -1;

    private Queue<(uint remoteId, NetPacket packet, TransportMethod method)> queuedPackets = new Queue<(uint remoteId, NetPacket packet, TransportMethod method)>();
    private bool isConnecting = false;
    private bool isDisconnecting = false;

    void FixedUpdate()
    {
        PollEvents();
    }

    public override void Initialize()
    {
#nullable enable
        ClientManager.Instance.OnLobbyCreateRequested += (lobbyId, lobbySettings, serverSettings, gameServerToken) =>
        {
            StartClient();
        };

        ClientManager.Instance.OnLobbyJoinRequested += (lobbyId, lobbySettings, serverSettings, gameServerToken) =>
        {
            StartClient();
        };
#nullable disable
    }

    void PollEvents()
    {
        if (isConnecting)
        {
            isConnecting = false;
            RaiseNetworkConnected(0);
        }

        if (isDisconnecting)
        {
            isDisconnecting = false;
            RaiseNetworkDisconnected(0);
        }

        while (queuedPackets.Count > 0)
        {
            var (remoteId, packet, method) = queuedPackets.Dequeue();
            RaiseNetworkReceived(remoteId, packet, method);
        }
    }

    public override bool StartClient()
    {
        if (hostType != NetDeviceType.None)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Already started as " + hostType);
            return false;
        }

        if (!UpdateInstances())
        {
            return false;
        }

        hostType = NetDeviceType.Client;

        if (instances[0] != null && instances[1] != null && instances[0].HostType != NetDeviceType.None && instances[1].HostType != NetDeviceType.None)
        {
            instances[1 - instanceIndex].isConnecting = true;
            instances[instanceIndex].isConnecting = true;
        }
        return true;
    }

    public override bool StartServer()
    {
        if (hostType != NetDeviceType.None)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Already started as " + hostType);
            return false;
        }

        if (!UpdateInstances())
        {
            return false;
        }

        hostType = NetDeviceType.Server;

        if (instances[0] != null && instances[1] != null && instances[0].HostType != NetDeviceType.None && instances[1].HostType != NetDeviceType.None)
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
            Debug.LogError("<color=red><b>CNS</b></color>: No other LocalTransport instance found to send data to.");
            return;
        }

        otherInstance.queuedPackets.Enqueue((remoteId, packet, method));
    }

    public override void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod method)
    {
        Send(0, packet, method);
    }

    public override void SendToAll(NetPacket packet, TransportMethod method)
    {
        Send(0, packet, method);
    }

    public override void Disconnect()
    {
        if (hostType == NetDeviceType.Server)
        {
            isDisconnecting = true;
            instances[1 - instanceIndex].isDisconnecting = true;
        }
        else if (hostType == NetDeviceType.Client)
        {
            instances[1 - instanceIndex].isDisconnecting = true;
            isDisconnecting = true;
        }
    }

    public override void DisconnectRemote(uint remoteId)
    {
        Disconnect();
    }

    public override void Shutdown()
    {
        Disconnect();
        instances[instanceIndex] = null;
        instanceIndex = -1;
        hostType = NetDeviceType.None;
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
