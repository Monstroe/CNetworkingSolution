using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class InteractableClientService : ClientService
{
    public Dictionary<ushort, ClientInteractable> ClientInteractables { get; private set; } = new Dictionary<ushort, ClientInteractable>();

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }
}
