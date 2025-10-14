using UnityEngine;

public class LobbyServerService : ServerService
{
    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
#if !CNS_LOBBY_SINGLE
            case CommandType.LOBBY_SETTINGS:
                {
                    if (!user.IsHost(lobby.LobbyData))
                    {
                        Debug.LogWarning($"User {user.UserId} tried to set lobby settings, but only the host can change lobby settings.");
                        return;
                    }

                    LobbySettings lobbySettings = new LobbySettings().Deserialize(packet);
                    lobby.LobbyData.Settings = lobbySettings;
                    lobby.SendToLobby(PacketBuilder.LobbySettings(lobbySettings), transportMethod ?? TransportMethod.Reliable);
#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
                    if (GameResources.Instance.GameMode != GameMode.Singleplayer)
                    {
                        ServerManager.Instance.Database.UpdateLobbyMetadataAsync(lobby.LobbyData);
                    }
#endif
                    break;
                }
#endif
            case CommandType.LOBBY_USER_SETTINGS:
                {
                    ulong userId = packet.ReadULong();
                    if (userId != user.UserId)
                    {
                        Debug.LogWarning($"User {user.UserId} tried to set settings for user {userId}, but only the user themselves can set their own settings.");
                        return;
                    }

                    UserSettings userSettings = new UserSettings().Deserialize(packet);
                    user.Settings = userSettings;
                    lobby.SendToLobby(PacketBuilder.LobbyUserSettings(user, userSettings), transportMethod ?? TransportMethod.Reliable);
#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
                    if (GameResources.Instance.GameMode != GameMode.Singleplayer)
                    {
                        ServerManager.Instance.Database.UpdateUserMetadataAsync(user);
                    }
#endif
                    break;
                }
        }
    }

    public override void Tick(ServerLobby lobby)
    {
        // Nothing
    }

    public override void UserJoined(ServerLobby lobby, UserData joinedUser)
    {
        lobby.SendToUser(joinedUser, PacketBuilder.LobbySettings(lobby.LobbyData.Settings), TransportMethod.Reliable);
        lobby.SendToUser(joinedUser, PacketBuilder.LobbyUsersList(lobby.LobbyData.LobbyUsers), TransportMethod.Reliable);
        lobby.SendToUser(joinedUser, PacketBuilder.LobbyTick(ServerManager.Instance.ServerTick), TransportMethod.Reliable);
        lobby.SendToLobby(PacketBuilder.LobbyUserJoined(joinedUser), TransportMethod.Reliable, joinedUser);
    }

    public override void UserJoinedGame(ServerLobby lobby, UserData joinedUser)
    {
        // Nothing
    }

    public override void UserLeft(ServerLobby lobby, UserData leftUser)
    {
        lobby.SendToLobby(PacketBuilder.LobbyUserLeft(leftUser), TransportMethod.Reliable, leftUser);
    }
}
