using System.Collections.Generic;

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
                    ClientManager.Instance.SetClientTick(tick);
                    break;
                }
            case CommandType.LOBBY_SETTINGS:
                {
                    LobbySettings settings = new LobbySettings().Deserialize(ref packet);
                    ClientManager.Instance.UpdateCurrentLobby(settings, ClientManager.Instance.CurrentUser.IsHost, false);
                    ClientManager.Instance.CurrentLobby.LobbyData.Settings = settings;
                    break;
                }
            case CommandType.LOBBY_USER_SETTINGS:
                {
                    ulong userId = packet.ReadULong();
                    UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.UserId == userId);
                    UserSettings userSettings = new UserSettings().Deserialize(ref packet);

                    if (userId == ClientManager.Instance.CurrentUser.UserId)
                    {
                        ClientManager.Instance.UpdateCurrentUser(userSettings, true, false);
                    }
                    else
                    {
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
                        UserData user = new UserData().Deserialize(ref packet);
                        updatedUsers.Add(user);
                    }

                    ClientManager.Instance.SetCurrentUserData(updatedUsers[updatedUsers.Count - 1]); // Set the local user data
                    ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.AddRange(updatedUsers);
                    break;
                }
            case CommandType.LOBBY_USER_JOINED:
                {
                    UserData user = new UserData().Deserialize(ref packet);
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
