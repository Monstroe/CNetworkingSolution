using UnityEngine;

public abstract class ClientService : MonoBehaviour
{
    public abstract void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod);
}