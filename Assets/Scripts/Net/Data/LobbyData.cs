using System;
using System.Collections.Generic;
using UnityEngine;

public class LobbyData : INetSerializable<LobbyData>
{
    public int LobbyId { get; set; }
    public List<UserData> LobbyUsers { get; set; } = new List<UserData>();
    public List<UserData> GameUsers { get { return LobbyUsers.FindAll(u => u.InGame); } }
    public int UserCount { get { return LobbyUsers.Count; } }
    public UserData HostUser { get { return LobbyUsers.Count > 0 ? LobbyUsers[0] : null; } }
    public LobbySettings Settings { get; set; } = new LobbySettings();

    public LobbyData Deserialize(NetPacket packet)
    {
        int lobbyId = packet.ReadInt();
        int userCount = packet.ReadInt();
        List<UserData> users = new List<UserData>(userCount);
        for (int i = 0; i < userCount; i++)
        {
            UserData user = new UserData().Deserialize(packet);
            users.Add(user);
        }
        return new LobbyData()
        {
            LobbyId = lobbyId,
            LobbyUsers = users,
            Settings = new LobbySettings().Deserialize(packet)
        };
    }

    public void Serialize(NetPacket packet)
    {
        packet.Write(LobbyId);
        packet.Write(LobbyUsers.Count);
        foreach (UserData user in LobbyUsers)
        {
            user.Serialize(packet);
        }
        Settings.Serialize(packet);
    }
}

[Serializable]
public class LobbySettings : INetSerializable<LobbySettings>
{
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
    public ulong SteamCode { get => steamCode; set => steamCode = value; }
#endif
    public int MaxUsers { get => maxUsers; set => maxUsers = value; }
    public LobbyVisibility LobbyVisibility { get => lobbyVisibility; set => lobbyVisibility = value; }
    public string LobbyName { get => lobbyName; set => lobbyName = value; }

#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
    [SerializeField] private ulong steamCode;
#endif
    [SerializeField] private int maxUsers;
    [SerializeField] private LobbyVisibility lobbyVisibility;
    [SerializeField] private string lobbyName;

    public LobbySettings Deserialize(NetPacket packet)
    {
        return new LobbySettings()
        {
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
            SteamCode = packet.ReadULong(),
#endif
            MaxUsers = packet.ReadInt(),
            LobbyVisibility = (LobbyVisibility)packet.ReadByte(),
            LobbyName = packet.ReadString(),
        };
    }

    public void Serialize(NetPacket packet)
    {
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
        packet.Write(SteamCode);
#endif
        packet.Write(MaxUsers);
        packet.Write((byte)LobbyVisibility);
        packet.Write(LobbyName);
    }
}

public enum LobbyVisibility
{
    PUBLIC,
    PRIVATE
}