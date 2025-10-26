using UnityEngine;

public abstract class ClientService : ClientBehaviour
{
    public abstract void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod);
}