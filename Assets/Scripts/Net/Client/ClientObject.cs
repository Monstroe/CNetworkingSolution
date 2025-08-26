using UnityEngine;

public abstract class ClientObject : MonoBehaviour, NetObject
{
    public ushort Id { get; protected set; }

    public virtual void Init(ushort id)
    {
        Id = id;
    }

    public abstract void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod);
}
