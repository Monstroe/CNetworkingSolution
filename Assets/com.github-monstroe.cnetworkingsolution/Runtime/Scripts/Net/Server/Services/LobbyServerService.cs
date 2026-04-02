using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class LobbyServerService : ServerService
{
    public delegate void LobbyUserKickedEventHandler(ulong userId, string kickReason);
    public event LobbyUserKickedEventHandler OnLobbyUserKicked;

    // The lobby service is also special because it handles lobby and user management
    // It needs to run last because the clients shouldn't clean up their UserData until all other services have processed the user leaving
    // Therefore THIS SERVER SERVICE SHOULD ALWAYS BE ADDED LAST, DON'T ADD ANYTHING AFTER THIS
    public override void Init(ServerLobby lobby)
    {
        base.Init(lobby);
    }

    public override void ReceiveData(UserData user, NetPacket packet, ushort commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.LOBBY_USER_KICK:
                {
                    if (!user.IsHost(lobby.LobbyData))
                    {
                        Debug.LogWarning($"User {user.UserId} tried to kick a user, but only the host can kick users.");
                        return;
                    }

                    ulong kickUserId = packet.ReadULong();
                    string kickReason = packet.ReadString();
                    UserData kickUser = lobby.LobbyData.LobbyUsers.Find(u => u.UserId == kickUserId);
                    if (kickUser == null)
                    {
                        Debug.LogWarning($"User {user.UserId} tried to kick user {kickUserId}, but that user was not found in the lobby.");
                        return;
                    }

                    OnLobbyUserKicked?.Invoke(kickUserId, kickReason);
                    lobby.SendToLobby(LobbyUserKick(kickUser, kickReason), TransportMethod.Reliable);
                    lobby.KickUser(kickUser);
                    break;
                }
#endif
                /*case CommandType.LOBBY_USER_SETTINGS:
                    {
                        ulong userId = packet.ReadULong();
                        if (userId != user.UserId)
                        {
                            Debug.LogWarning($"User {user.UserId} tried to set settings for user {userId}, but only the user themselves can set their own settings.");
                            return;
                        }

                        UserSettings userSettings = new UserSettings().Deserialize(packet);
                        user.Settings = userSettings;
                        lobby.SendToLobby(PacketBuilder.LobbyUserSettings(user, userSettings, true), TransportMethod.Reliable);
    #if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
                        if (NetResources.Instance.NetMode != NetMode.Local)
                        {
                            ServerManager.Instance.Database.UpdateUserMetadataAsync(user);
                        }
    #endif
                        break;
                    }*/
        }
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ushort commandType)
    {
        // Nothing
    }

    public override void Tick()
    {
        // Nothing
    }

    public override void UserJoined(UserData joinedUser)
    {
        //lobby.SendToUser(joinedUser, PacketBuilder.LobbySettings(lobby.LobbyData.Settings, false), TransportMethod.Reliable);
        lobby.SendToUser(joinedUser, LobbyPacketBuilder.LobbyUsersList(lobby.LobbyData.LobbyUsers), TransportMethod.Reliable);
        lobby.SendToUser(joinedUser, LobbyPacketBuilder.LobbyTick(lobby.ServerTick, true), TransportMethod.Reliable);
        lobby.SendToLobby(LobbyPacketBuilder.LobbyUserJoined(joinedUser), TransportMethod.Reliable, joinedUser);
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserLeft(UserData leftUser)
    {
        lobby.SendToLobby(LobbyPacketBuilder.LobbyUserLeft(leftUser), TransportMethod.Reliable, leftUser);
    }
}
