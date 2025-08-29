using UnityEngine;

public class LobbyServerService : ServerService
{
    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
            case CommandType.LOBBY_SETTINGS:
                {
                    if (!user.IsHost)
                    {
                        Debug.LogWarning($"User {user.UserId} tried to set lobby settings, but only the host can change lobby settings.");
                        return;
                    }

                    LobbySettings lobbySettings = new LobbySettings().Deserialize(ref packet);
                    lobby.LobbyData.Settings = lobbySettings;
                    lobby.SendToLobby(PacketBuilder.LobbySettings(lobbySettings), transportMethod ?? TransportMethod.Reliable);
                    break;
                }
#endif
            // TEMP: REMOVE THIS LATER
            case CommandType.LOBBY_SETTINGS:
                {
                    if (!user.IsHost)
                    {
                        Debug.LogWarning($"User {user.UserId} tried to set lobby settings, but only the host can change lobby settings.");
                        return;
                    }

                    LobbySettings lobbySettings = new LobbySettings().Deserialize(ref packet);
                    lobby.LobbyData.Settings = lobbySettings;
                    lobby.SendToLobby(PacketBuilder.LobbySettings(lobbySettings), transportMethod ?? TransportMethod.Reliable);
                    break;
                }
            case CommandType.LOBBY_USER_SETTINGS:
                {
                    ulong userId = packet.ReadULong();
                    if (userId != user.UserId)
                    {
                        Debug.LogWarning($"User {user.UserId} tried to set settings for user {userId}, but only the user themselves can set their own settings.");
                        return;
                    }

                    UserSettings userSettings = new UserSettings().Deserialize(ref packet);
                    user.Settings = userSettings;
                    lobby.SendToLobby(PacketBuilder.LobbyUserSettings(user, userSettings), transportMethod ?? TransportMethod.Reliable);
                    break;
                }
        }
    }

    public override void Tick(ServerLobby lobby)
    {

    }

    public override void UserJoined(ServerLobby lobby, UserData user)
    {
        lobby.SendToUser(user, PacketBuilder.LobbySettings(lobby.LobbyData.Settings), TransportMethod.Reliable);
        lobby.SendToUser(user, PacketBuilder.LobbyUsersList(lobby.LobbyData.LobbyUsers), TransportMethod.Reliable);
        lobby.SendToUser(user, PacketBuilder.LobbyTick(ServerManager.Instance.ServerTick), TransportMethod.Reliable);
        lobby.SendToLobby(PacketBuilder.LobbyUserJoined(user), TransportMethod.Reliable, user);
    }

    public override void UserJoinedGame(ServerLobby lobby, UserData user)
    {

    }

    public override void UserLeft(ServerLobby lobby, UserData user)
    {
        lobby.SendToLobby(PacketBuilder.LobbyUserLeft(user), TransportMethod.Reliable, user);
    }
}
