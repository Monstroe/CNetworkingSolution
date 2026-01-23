using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class EntityClientService : ClientService
{
    public Dictionary<ushort, ClientEntity> ClientEntities { get; private set; } = new Dictionary<ushort, ClientEntity>();

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }
}
