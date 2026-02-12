using System.Net;
using UnityEngine;

public class ChatClientService : ClientService
{
    public ClientChat Chat { get; set; }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }
}
