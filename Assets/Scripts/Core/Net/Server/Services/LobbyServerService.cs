using System.Net;
using UnityEngine;

public class LobbyServerService : ServerService
{
    // The lobby service is also special because it handles lobby and user management
    // It needs to run last because the clients shouldn't clean up their UserData until all other services have processed the user leaving
    // Therefore THIS SERVER SERVICE SHOULD ALWAYS BE ADDED LAST, DON'T ADD ANYTHING AFTER THIS
    public override void Init(ServerLobby lobby)
    {
        base.Init(lobby);
    }

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
#if !CNS_LOBBY_SINGLE || (CNS_LOBBY_SINGLE && CNS_SYNC_HOST)
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
                    if (NetResources.Instance.GameMode != GameMode.Singleplayer)
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
                    if (NetResources.Instance.GameMode != GameMode.Singleplayer)
                    {
                        ServerManager.Instance.Database.UpdateUserMetadataAsync(user);
                    }
#endif
                    break;
                }
        }
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }

    public override void Tick()
    {
        // Nothing
    }

    public override void UserJoined(UserData joinedUser)
    {
        lobby.SendToUser(joinedUser, PacketBuilder.LobbySettings(lobby.LobbyData.Settings), TransportMethod.Reliable);
        lobby.SendToUser(joinedUser, PacketBuilder.LobbyUsersList(lobby.LobbyData.LobbyUsers), TransportMethod.Reliable);
        lobby.SendToUser(joinedUser, PacketBuilder.LobbyTick(lobby.ServerTick), TransportMethod.Reliable);
        lobby.SendToLobby(PacketBuilder.LobbyUserJoined(joinedUser), TransportMethod.Reliable, joinedUser);
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserLeft(UserData leftUser)
    {
        lobby.SendToLobby(PacketBuilder.LobbyUserLeft(leftUser), TransportMethod.Reliable, leftUser);
    }
}
