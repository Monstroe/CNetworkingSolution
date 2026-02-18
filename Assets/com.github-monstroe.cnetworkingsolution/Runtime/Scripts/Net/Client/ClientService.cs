using System.Net;
using UnityEngine;

public abstract class ClientService : ClientBehaviour
{
    public uint ServiceId { get; private set; }

    public override void Init(ClientLobby lobby)
    {
        base.Init(lobby);
        uint? serviceId = lobby.RegisterService(this);
        if (serviceId.HasValue)
        {
            ServiceId = serviceId.Value;
            Debug.Log($"<color=green><b>CNS</b></color>: ClientService {GetType().Name} registered with id {serviceId.Value}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {GetType().Name} is already registered.");
        }
    }

    public abstract void ReceiveData(NetPacket packet, CommandType commandType, TransportMethod? transportMethod);
    public abstract void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, CommandType commandType);
}