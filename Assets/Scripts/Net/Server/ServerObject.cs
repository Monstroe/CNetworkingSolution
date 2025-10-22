using UnityEngine;

public abstract class ServerObject : MonoBehaviour, INetObject
{
    public ushort Id { get; protected set; }
    protected ServerLobby lobby;

    public virtual void Init(ushort id, ServerLobby lobby)
    {
        this.Id = id;
        this.lobby = lobby;
    }

    public abstract void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod);
    public abstract void Tick();
    public abstract void UserJoined(UserData joinedUser);
    public abstract void UserJoinedGame(UserData joinedUser);
    public abstract void UserLeft(UserData leftUser);
}