#if CNS_TRANSPORT_LOCAL
using System.Collections.Generic;
using UnityEngine;

public class LocalTransport : NetTransport
{
    private static LocalTransport clientInstance;
    private static LocalTransport serverInstance;

    void Awake()
    {
        if (clientInstance == null)
        {
            clientInstance = this;
        }
        else if (serverInstance == null)
        {
            serverInstance = this;
        }
        else
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: More than 2 instances of SinglePlayerTransport detected. Destroying extra instance.");
            Destroy(gameObject);
        }
    }

    public override void Initialize()
    {
        ClientManager.Instance.OnLobbyCreateRequested += (lobbyId, serverSettings, gameServerToken) =>
        {
            StartClient();
        };

        ClientManager.Instance.OnLobbyJoinRequested += (lobbyId, serverSettings, gameServerToken) =>
        {
            StartClient();
        };
    }

    public override bool StartClient()
    {
        if (hostType != NetDeviceType.None)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Already started as " + hostType);
            return false;
        }

        hostType = NetDeviceType.Client;
        return true;
    }

    public override bool StartServer()
    {
        if (hostType != NetDeviceType.None)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Already started as " + hostType);
            return false;
        }

        hostType = NetDeviceType.Server;
        serverInstance.RaiseNetworkConnected(0);
        clientInstance.RaiseNetworkConnected(0);
        return true;
    }

    public override void Send(uint remoteId, NetPacket packet, TransportMethod method)
    {
        if (clientInstance == this)
        {
            serverInstance.RaiseNetworkReceived(remoteId, packet, method);
        }
        else if (serverInstance == this)
        {
            clientInstance.RaiseNetworkReceived(remoteId, packet, method);
        }
        else
        {
            Debug.LogError("<color=red><b>CNS</b></color>: SinglePlayerTransport is not initialized correctly.");
        }
    }

    public override void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod method)
    {
        if (remoteIds.Count == 1)
        {
            Send(remoteIds[0], packet, method);
        }
    }

    public override void SendToAll(NetPacket packet, TransportMethod method)
    {
        Send(0, packet, method);
    }

    public override void Disconnect()
    {
        serverInstance.RaiseNetworkDisconnected(0);
        clientInstance.RaiseNetworkDisconnected(0);
    }

    public override void DisconnectRemote(uint remoteId)
    {
        Disconnect();
    }

    public override void Shutdown()
    {
        Disconnect();
        clientInstance = null;
        serverInstance = null;
        hostType = NetDeviceType.None;
    }
}
#endif
