using UnityEngine;

public class LobbyServerService : ServerService
{
    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void Tick(ServerLobby lobby)
    {
        // Nothing
    }

    public override void UserJoined(ServerLobby lobby, UserData user)
    {
        lobby.SendToRoom(PacketBuilder.LobbyUserJoined(user), TransportMethod.Reliable, user);
        lobby.SendToUser(user, PacketBuilder.LobbyUsers(lobby.LobbyData.LobbyUsers), TransportMethod.Reliable);
    }

    public override void UserLeft(ServerLobby lobby, UserData user)
    {
        lobby.SendToRoom(PacketBuilder.LobbyUserLeft(user), TransportMethod.Reliable, user);
    }
}
