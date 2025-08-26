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
#if CNS_TRANSPORT_STEAMWORKS
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
                    uint userId = packet.ReadUInt();
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
                            UserId = packet.ReadUInt(),
                            PlayerId = packet.ReadByte(),
                            Settings = new UserSettings()
                            {
                                UserName = packet.ReadString()
                            }
                        };
                        updatedUsers.Add(user);
                    }

                    ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers = updatedUsers;
                    break;
                }
            case CommandType.LOBBY_USER_JOINED:
                {
                    Debug.Log("User Joined Lobby");
                    UserData user = new UserData()
                    {
                        GlobalGuid = Guid.Parse(packet.ReadString()),
                        UserId = packet.ReadUInt(),
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
                    uint userId = packet.ReadUInt();
                    ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.RemoveAll(u => u.UserId == userId);
                    break;
                }
        }
    }
}
