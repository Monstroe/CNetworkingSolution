using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class LobbyClientService : ClientService
{
    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.LOBBY_SETTINGS:
                {
                    LobbySettings settings = new LobbySettings()
                    {
                        InternalCode = packet.ReadULong(),
                        MaxUsers = packet.ReadInt(),
                        LobbyVisibility = (LobbyVisibility)packet.ReadByte(),
                        LobbyName = packet.ReadString(),
                    };

                    ClientLobby.Instance.LobbyData.Settings = settings;
                    break;
                }
            case CommandType.LOBBY_USERS:
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

                    ClientLobby.Instance.LobbyData.LobbyUsers = updatedUsers;
                    break;
                }
            case CommandType.LOBBY_USER_JOINED:
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
                    ClientLobby.Instance.LobbyData.LobbyUsers.Add(user);
                    break;
                }
            case CommandType.LOBBY_USER_LEFT:
                {
                    uint userId = packet.ReadUInt();
                    ClientLobby.Instance.LobbyData.LobbyUsers.RemoveAll(u => u.UserId == userId);
                    break;
                }
        }
    }
}
