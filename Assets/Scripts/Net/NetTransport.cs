using System.Collections.Generic;
using UnityEngine;

public abstract class NetTransport : MonoBehaviour
{
    /*private static NetTransport instance;

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Multiple NetTransport instances found, destroying one.");
            Destroy(gameObject);
        }
    }*/

    public NetDeviceType HostType => hostType;
    public virtual uint ServerClientId { get; }
    public virtual List<uint> ConnectedClientIds { get; }

    protected NetDeviceType hostType = NetDeviceType.None;

    public abstract void Initialize();
    public abstract bool StartClient();
    public abstract bool StartServer();
    public abstract void Send(uint remoteId, NetPacket packet, TransportMethod method);
    public abstract void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod method);
    public abstract void SendToAll(NetPacket packet, TransportMethod method);
    public abstract void Disconnect();
    public abstract void DisconnectRemote(uint remoteId);
    public abstract void Shutdown();
}

public enum NetDeviceType
{
    None,
    Server,
    Client
}

public enum TransportMethod
{
    None,
    Reliable,
    ReliableUnordered,
    UnreliableSequenced,
    Unreliable,
}
