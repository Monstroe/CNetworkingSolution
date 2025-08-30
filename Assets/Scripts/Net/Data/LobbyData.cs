using System;
using System.Collections.Generic;
using UnityEngine;

public class LobbyData
{
    public int LobbyId { get; set; }
    public List<UserData> LobbyUsers { get; set; } = new List<UserData>();
    public List<UserData> GameUsers { get { return LobbyUsers.FindAll(u => u.InGame); } }
    public int UserCount { get { return LobbyUsers.Count; } }
    public UserData HostUser { get { return LobbyUsers.Count > 0 ? LobbyUsers[0] : null; } }
    public LobbySettings Settings { get; set; } = new LobbySettings();
}

[Serializable]
public class LobbySettings : INetSerializable<LobbySettings>
{
#if CNS_TRANSPORT_STEAMWORKS && CNS_HOST_AUTH
    public ulong SteamCode { get => steamCode; set => steamCode = value; }
#endif
    public int MaxUsers { get => maxUsers; set => maxUsers = value; }
    public LobbyVisibility LobbyVisibility { get => lobbyVisibility; set => lobbyVisibility = value; }
    public string LobbyName { get => lobbyName; set => lobbyName = value; }

#if CNS_TRANSPORT_STEAMWORKS && CNS_HOST_AUTH
    [SerializeField] private ulong steamCode;
#endif
    [SerializeField] private int maxUsers;
    [SerializeField] private LobbyVisibility lobbyVisibility;
    [SerializeField] private string lobbyName;

    public LobbySettings Deserialize(ref NetPacket packet)
    {
        return new LobbySettings()
        {
#if CNS_TRANSPORT_STEAMWORKS && CNS_HOST_AUTH
            SteamCode = packet.ReadULong(),
#endif
            MaxUsers = packet.ReadInt(),
            LobbyVisibility = (LobbyVisibility)packet.ReadByte(),
            LobbyName = packet.ReadString(),
        };
    }

    public void Serialize(ref NetPacket packet)
    {
#if CNS_TRANSPORT_STEAMWORKS && CNS_HOST_AUTH
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