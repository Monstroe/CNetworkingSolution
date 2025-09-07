using System.Collections.Generic;
using UnityEngine;

public class LobbyClientService : ClientService
{
    void Start()
    {
        ClientManager.Instance.CurrentLobby.RegisterService(ServiceType.LOBBY, this);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.LOBBY_TICK:
                {
                    ulong tick = packet.ReadULong();
                    ClientManager.Instance.SetClientTick(tick); // Update the client tick
                    break;
                }
            case CommandType.LOBBY_SETTINGS:
                {
                    LobbySettings settings = new LobbySettings().Deserialize(packet);
                    ClientManager.Instance.UpdateCurrentLobby(settings, ClientManager.Instance.CurrentUser.IsHost(ClientManager.Instance.CurrentLobby.LobbyData), false);
                    ClientManager.Instance.CurrentLobby.LobbyData.Settings = settings;
                    Debug.Log($"Lobby settings changed: MaxUsers: {settings.MaxUsers}, LobbyVisibility: {settings.LobbyVisibility}, LobbyName: {settings.LobbyName}");
                    break;
                }
            case CommandType.LOBBY_USER_SETTINGS:
                {
                    ulong userId = packet.ReadULong();
                    UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.UserId == userId);
                    UserSettings userSettings = new UserSettings().Deserialize(packet);

                    if (userId == ClientManager.Instance.CurrentUser.UserId)
                    {
                        ClientManager.Instance.UpdateCurrentUser(userSettings, true, false);
                    }
                    else
                    {
                        Debug.Log($"Updating settings for remote user {userId}: UserName: {userSettings.UserName}");
                        user.Settings = userSettings;
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

                    ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.AddRange(updatedUsers);
                    ClientManager.Instance.SetCurrentUserData(updatedUsers[updatedUsers.Count - 1]); // Set the local user data
                    break;
                }
            case CommandType.LOBBY_USER_JOINED:
                {
                    UserData user = new UserData().Deserialize(packet);
                    ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Add(user);
                    break;
                }
            case CommandType.LOBBY_USER_LEFT:
                {
                    ulong userId = packet.ReadULong();
                    ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.RemoveAll(u => u.UserId == userId);
                    break;
                }
        }
    }
}
