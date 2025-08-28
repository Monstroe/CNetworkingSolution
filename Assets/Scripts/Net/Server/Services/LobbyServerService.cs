using UnityEngine;

public class LobbyServerService : ServerService
{
    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.LOBBY_SETTINGS:
                {
                    LobbySettings lobbySettings = new LobbySettings()
                    {
#if CNS_TRANSPORT_STEAMWORKS && CNS_HOST_AUTH
                        SteamCode = packet.ReadULong(),
#endif
                        MaxUsers = packet.ReadInt(),
                        LobbyVisibility = (LobbyVisibility)packet.ReadByte(),
                        LobbyName = packet.ReadString()
                    };
                    lobby.LobbyData.Settings = lobbySettings;
                    lobby.SendToLobby(PacketBuilder.LobbySettings(lobbySettings), transportMethod ?? TransportMethod.Reliable, user);
                    break;
                }
            case CommandType.LOBBY_USER_SETTINGS:
                {
                    ulong userId = packet.ReadULong();
                    UserData thisUser = lobby.LobbyData.LobbyUsers.Find(u => u.UserId == userId);
                    if (thisUser != user)
                    {
                        Debug.LogWarning($"User {user.UserId} tried to set settings for user {thisUser.UserId}, but only the user themselves can set their own settings.");
                        return;
                    }

                    UserSettings userSettings = new UserSettings()
                    {
                        UserName = packet.ReadString()
                    };
                    user.Settings = userSettings;
                    lobby.SendToLobby(PacketBuilder.LobbyUserSettings(user, userSettings), transportMethod ?? TransportMethod.Reliable, user);
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
        Debug.Log($"User {user.UserId} ({user.PlayerId}) joined lobby {lobby.LobbyData.LobbyId}");
    }

    public override void UserJoinedGame(ServerLobby lobby, UserData user)
    {

    }

    public override void UserLeft(ServerLobby lobby, UserData user)
    {
        lobby.SendToLobby(PacketBuilder.LobbyUserLeft(user), TransportMethod.Reliable, user);
    }
}
