using UnityEngine;

public abstract class ServerService : MonoBehaviour
{
    public abstract void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod);
    public abstract void Tick(ServerLobby lobby);
    public abstract void UserJoined(ServerLobby lobby, UserData joinedUser);
    public abstract void UserJoinedGame(ServerLobby lobby, UserData joinedUser);
    public abstract void UserLeft(ServerLobby lobby, UserData leftUser);
}