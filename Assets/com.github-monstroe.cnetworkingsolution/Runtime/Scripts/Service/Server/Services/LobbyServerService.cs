[ServiceId("LobbyService")]
public class LobbyServerService : ServerService
{
    // The lobby service is also special because it handles lobby and user management
    // It needs to run last because the clients shouldn't clean up their UserData until all other services have processed the user leaving
    // Therefore THIS SERVER SERVICE SHOULD ALWAYS BE ADDED LAST, DON'T ADD ANYTHING AFTER THIS
    public override void Init(ServerLobby lobby)
    {
        base.Init(lobby);
    }

    public override void UserJoined(UserData joinedUser)
    {
        base.UserJoined(joinedUser);
        SendToUserClientService(joinedUser, LobbyPacketBuilder.LobbyInit(lobby.ServerTick, lobby.LobbyData.LobbyUsers), TransportMethod.Reliable);
        SendToLobbyClientServices(LobbyPacketBuilder.LobbyUserJoined(joinedUser), TransportMethod.Reliable, joinedUser);
    }

    public override void UserLeft(UserData leftUser)
    {
        base.UserLeft(leftUser);
        SendToLobbyClientServices(LobbyPacketBuilder.LobbyUserLeft(leftUser), TransportMethod.Reliable, leftUser);
    }
}
