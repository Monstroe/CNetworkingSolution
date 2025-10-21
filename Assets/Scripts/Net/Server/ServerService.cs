using UnityEngine;

public abstract class ServerService : MonoBehaviour
{
    protected ServerLobby lobby;

    public virtual void Init(ServerLobby lobby)
    {
        this.lobby = lobby;
    }

    public abstract void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod);
    public abstract void Tick();
    public abstract void UserJoined(UserData joinedUser);
    public abstract void UserJoinedGame(UserData joinedUser);
    public abstract void UserLeft(UserData leftUser);
}