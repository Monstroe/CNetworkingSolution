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

    public override void ReceiveData(UserData user, NetPacket packet, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
#if !CNS_LOBBY_SINGLE || (CNS_LOBBY_SINGLE && CNS_SYNC_HOST)
            /*case CommandType.LOBBY_SETTINGS:
                {
                    if (!user.IsHost(lobby.LobbyData))
                    {
                        Debug.LogWarning($"User {user.UserId} tried to set lobby settings, but only the host can change lobby settings.");
                        return;
                    }

                    LobbySettings lobbySettings = new LobbySettings().Deserialize(packet);
                    lobby.LobbyData.Settings = lobbySettings;
                    lobby.SendToLobby(PacketBuilder.LobbySettings(lobbySettings, true), TransportMethod.Reliable);
#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
                    if (NetResources.Instance.NetMode != NetMode.Local)
                    {
                        ServerManager.Instance.Database.UpdateLobbyMetadataAsync(lobby.LobbyData);
                    }
#endif
                    break;
                }*/
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

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, CommandType commandType)
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
        lobby.SendToUser(joinedUser, LobbyUsersList(lobby.LobbyData.LobbyUsers), TransportMethod.Reliable);
        lobby.SendToUser(joinedUser, LobbyTick(lobby.ServerTick, true), TransportMethod.Reliable);
        lobby.SendToLobby(LobbyUserJoined(joinedUser), TransportMethod.Reliable, joinedUser);
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserLeft(UserData leftUser)
    {
        lobby.SendToLobby(LobbyUserLeft(leftUser), TransportMethod.Reliable, leftUser);
    }

    /* PACKETS */

    public enum LobbyCommandType
    {
        LOBBY_SETTINGS,
        LOBBY_USER_SETTINGS,
        LOBBY_USERS_LIST,
        LOBBY_USER_JOINED,
        LOBBY_USER_LEFT,
#if !CNS_LOBBY_SINGLE || (CNS_LOBBY_SINGLE && CNS_SYNC_HOST)
        LOBBY_USER_KICK,
#endif
        LOBBY_TICK
    }

    public static NetPacket LobbyUsersList(List<UserData> users)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<LobbyClientService>());
        packet.Write((byte)LobbyCommandType.LOBBY_USERS_LIST);
        packet.Write((byte)users.Count);
        foreach (UserData user in users)
        {
            user.Serialize(packet);
        }
        return packet;
    }

    public static NetPacket LobbyUserJoined(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<LobbyClientService>());
        packet.Write((byte)LobbyCommandType.LOBBY_USER_JOINED);
        user.Serialize(packet);
        return packet;
    }

    public static NetPacket LobbyUserLeft(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<LobbyClientService>());
        packet.Write((byte)LobbyCommandType.LOBBY_USER_LEFT);
        packet.Write(user.UserId);
        return packet;
    }

#if !CNS_LOBBY_SINGLE || (CNS_LOBBY_SINGLE && CNS_SYNC_HOST)
    public static NetPacket LobbyUserKick(UserData user, string reason)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<LobbyClientService>());
        packet.Write((byte)LobbyCommandType.LOBBY_USER_KICK);
        packet.Write(user.UserId);
        packet.Write(reason);
        return packet;
    }
#endif

    public static NetPacket LobbyTick(ulong tick, bool invokeEvent = false)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<LobbyClientService>());
        packet.Write((byte)LobbyCommandType.LOBBY_TICK);
        packet.Write(tick);
        packet.Write(invokeEvent);
        return packet;
    }
}
