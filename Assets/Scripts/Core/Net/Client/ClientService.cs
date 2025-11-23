using System.Net;
using UnityEngine;

public abstract class ClientService : ClientBehaviour
{
    public ServiceType ServiceType => serviceType;

    [Header("Client Service Settings")]
    [SerializeField] private ServiceType serviceType;

    public virtual void Init(ClientLobby lobby)
    {
        this.lobby = lobby;
        lobby.RegisterService(this);
    }

    public abstract void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod);
    public abstract void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType);
}