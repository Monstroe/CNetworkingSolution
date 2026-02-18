using System.Net;
using UnityEngine;

public abstract class ServerService : ServerBehaviour
{
    public uint ServiceId { get; private set; }
    public int ExecutionOrder => executionOrder;

    [Header("Server Service Settings")]
    [SerializeField] protected int executionOrder = 0;

    public override void Init(ServerLobby lobby)
    {
        base.Init(lobby);
        uint? serviceId = lobby.RegisterService(this);
        if (serviceId.HasValue)
        {
            ServiceId = serviceId.Value;
            Debug.Log($"<color=green><b>CNS</b></color>: ServerService {GetType().Name} registered with id {serviceId.Value}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {GetType().Name} is already registered.");
        }
    }

    public abstract void ReceiveData(UserData user, NetPacket packet, CommandType commandType, TransportMethod? transportMethod);
    public abstract void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, CommandType commandType);
    public abstract void Tick();
    public abstract void UserJoined(UserData joinedUser);
    public abstract void UserJoinedGame(UserData joinedUser);
    public abstract void UserLeft(UserData leftUser);
}