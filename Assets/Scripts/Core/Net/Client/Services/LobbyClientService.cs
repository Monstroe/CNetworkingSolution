using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class LobbyClientService : ClientService
{
    public delegate void LobbyInitializedEventHandler(ulong tick, LobbyData lobbyData);
    public event LobbyInitializedEventHandler OnLobbyInitialized;

    public delegate void LobbySettingsUpdatedEventHandler(LobbySettings settings);
    public event LobbySettingsUpdatedEventHandler OnLobbySettingsUpdated;

    public delegate void LobbyUserSettingsUpdatedEventHandler(ulong userId, UserSettings settings);
    public event LobbyUserSettingsUpdatedEventHandler OnLobbyUserSettingsUpdated;

    public delegate void LobbyUserJoinedEventHandler(UserData user);
    public event LobbyUserJoinedEventHandler OnLobbyUserJoined;

    public delegate void LobbyUserLeftEventHandler(UserData user);
    public event LobbyUserLeftEventHandler OnLobbyUserLeft;

    public delegate void LobbyUserKickedEventHandler(ulong userId, LobbyRejectionType rejectionType);
    public event LobbyUserKickedEventHandler OnLobbyUserKicked;

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.LOBBY_TICK:
                {
                    ulong tick = packet.ReadULong();
                    lobby.ClientTick = tick; // Update the client tick
                    bool invokeEvent = packet.ReadBool();
                    if (invokeEvent)
                    {
                        OnLobbyInitialized?.Invoke(tick, lobby.LobbyData); // Notify that the lobby has been initialized (lobby tick is received last)
                    }
                    break;
                }
            case CommandType.LOBBY_SETTINGS:
                {
                    LobbySettings settings = new LobbySettings().Deserialize(packet);
                    lobby.LobbyData.Settings = settings;
                    bool invokeEvent = packet.ReadBool();
                    if (invokeEvent)
                    {
                        Debug.Log("Lobby settings updated received: " + settings.MaxUsers + " max users.");
                        OnLobbySettingsUpdated?.Invoke(settings);
                    }
                    break;
                }
            case CommandType.LOBBY_USER_SETTINGS:
                {
                    ulong userId = packet.ReadULong();
                    UserData user = lobby.LobbyData.LobbyUsers.Find(u => u.UserId == userId);
                    UserSettings userSettings = new UserSettings().Deserialize(packet);
                    user.Settings = userSettings;
                    bool invokeEvent = packet.ReadBool();
                    if (invokeEvent)
                    {
                        Debug.Log($"User settings updated received for user {userId}: {userSettings.UserName}");
                        OnLobbyUserSettingsUpdated?.Invoke(userId, userSettings);
                    }
                    break;
                }
            case CommandType.LOBBY_USERS_LIST:
                {
                    int userCount = packet.ReadByte();
                    List<UserData> updatedUsers = new List<UserData>(userCount);
                    for (int i = 0; i < userCount; i++)
                    {
                        UserData user = new UserData().Deserialize(packet);
                        updatedUsers.Add(user);
                    }
                    lobby.LobbyData.LobbyUsers.AddRange(updatedUsers);
                    lobby.CurrentUser = updatedUsers[updatedUsers.Count - 1]; // Set the local user data
                    break;
                }
            case CommandType.LOBBY_USER_JOINED:
                {
                    UserData user = new UserData().Deserialize(packet);
                    lobby.LobbyData.LobbyUsers.Add(user);
                    OnLobbyUserJoined?.Invoke(user);
                    break;
                }
            case CommandType.LOBBY_USER_LEFT:
                {
                    ulong userId = packet.ReadULong();
                    UserData user = lobby.LobbyData.LobbyUsers.Find(u => u.UserId == userId);
                    lobby.LobbyData.LobbyUsers.Remove(user);
                    OnLobbyUserLeft?.Invoke(user);
                    break;
                }
            case CommandType.LOBBY_USER_KICK:
                {
                    ulong userId = packet.ReadULong();
                    LobbyRejectionType rejectionType = (LobbyRejectionType)packet.ReadByte();
                    OnLobbyUserKicked?.Invoke(userId, rejectionType);
                    break;
                }
        }
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }
}
