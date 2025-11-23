using System.Net;
using UnityEngine;

public abstract class ServerService : ServerBehaviour
{
    public ServiceType ServiceType => serviceType;
    public int ExecutionOrder => executionOrder;

    [Header("Server Service Settings")]
    [SerializeField] private ServiceType serviceType;
    [SerializeField] protected int executionOrder = 0;

    public virtual void Init(ServerLobby lobby)
    {
        this.lobby = lobby; ;
        lobby.RegisterService(this);
    }

    public abstract void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod);
    public abstract void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType);
    public abstract void Tick();
    public abstract void UserJoined(UserData joinedUser);
    public abstract void UserJoinedGame(UserData joinedUser);
    public abstract void UserLeft(UserData leftUser);
}