using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
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
                    int tick = packet.ReadInt();
                    ClientManager.Instance.InitClientTick(tick);
                    break;
                }
            case CommandType.LOBBY_SETTINGS:
                {
                    LobbySettings settings = new LobbySettings()
                    {
#if CNS_TRANSPORT_STEAMWORKS && CNS_HOST_AUTH
                        SteamCode = packet.ReadULong(),
#endif
                        MaxUsers = packet.ReadInt(),
                        LobbyVisibility = (LobbyVisibility)packet.ReadByte(),
                        LobbyName = packet.ReadString(),
                    };

                    ClientManager.Instance.CurrentLobby.LobbyData.Settings = settings;
                    break;
                }
            case CommandType.LOBBY_USER_SETTINGS:
                {
                    ulong userId = packet.ReadULong();
                    UserData thisUser = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.UserId == userId);
                    UserSettings userSettings = new UserSettings()
                    {
                        UserName = packet.ReadString()
                    };
                    thisUser.Settings = userSettings;
                    break;
                }
            case CommandType.LOBBY_USERS_LIST:
                {
                    int userCount = packet.ReadByte();
                    List<UserData> updatedUsers = new List<UserData>(userCount);
                    for (int i = 0; i < userCount; i++)
                    {
                        UserData user = new UserData()
                        {
                            GlobalGuid = Guid.Parse(packet.ReadString()),
                            UserId = packet.ReadULong(),
                            PlayerId = packet.ReadByte(),
                            Settings = new UserSettings()
                            {
                                UserName = packet.ReadString()
                            }
                        };
                        updatedUsers.Add(user);
                        Debug.Log($"User {user.PlayerId} added.");
                    }

                    UserData localUser = updatedUsers[updatedUsers.Count - 1];
                    ClientManager.Instance.CurrentUser.GlobalGuid = localUser.GlobalGuid;
                    ClientManager.Instance.CurrentUser.UserId = localUser.UserId;
                    ClientManager.Instance.CurrentUser.PlayerId = localUser.PlayerId;

                    Debug.Log($"Local user {localUser.PlayerId} added.");

                    ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.AddRange(updatedUsers);
                    break;
                }
            case CommandType.LOBBY_USER_JOINED:
                {
                    UserData user = new UserData()
                    {
                        GlobalGuid = Guid.Parse(packet.ReadString()),
                        UserId = packet.ReadULong(),
                        PlayerId = packet.ReadByte(),
                        Settings = new UserSettings()
                        {
                            UserName = packet.ReadString()
                        }
                    };
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
