using System.Net;
using UnityEngine;

public class RpcClientService : ClientService
{
    public RpcBus Bus { get; private set; } = new RpcBus();

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }
}
