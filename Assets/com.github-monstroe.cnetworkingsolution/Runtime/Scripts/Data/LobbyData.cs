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
public class LobbySettings : INetSerializable<LobbySettings>, IDeepClone<LobbySettings>
{
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
    public ulong SteamCode { get => steamCode; set => steamCode = value; }
#endif
    public int MaxUsers { get => maxUsers; set => maxUsers = value; }
    public OnlineSettings OnlineSettings { get => onlineSettings; set => onlineSettings = value; }
    public GameSettings GameSettings { get => gameSettings; set => gameSettings = value; }

#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
    [SerializeField] private ulong steamCode;
#endif
    [SerializeField] private int maxUsers;
    [SerializeField] private OnlineSettings onlineSettings;
    [SerializeField] private GameSettings gameSettings;

    public LobbySettings Clone()
    {
        return new LobbySettings()
        {
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
            SteamCode = this.SteamCode,
#endif
            MaxUsers = this.MaxUsers,
            OnlineSettings = this.OnlineSettings.Clone(),
            GameSettings = this.GameSettings.Clone()
        };
    }

    public LobbySettings Deserialize(NetPacket packet)
    {
        return new LobbySettings()
        {
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
            SteamCode = packet.ReadULong(),
#endif
            MaxUsers = packet.ReadInt(),
            OnlineSettings = new OnlineSettings().Deserialize(packet),
            GameSettings = new GameSettings().Deserialize(packet)
        };
    }

    public void Serialize(NetPacket packet)
    {
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
        packet.Write(SteamCode);
#endif
        packet.Write(MaxUsers);
        OnlineSettings.Serialize(packet);
        GameSettings.Serialize(packet);
    }
}

[Serializable]
public class OnlineSettings : INetSerializable<OnlineSettings>, IDeepClone<OnlineSettings>
{
    public LobbyVisibility LobbyVisibility { get => lobbyVisibility; set => lobbyVisibility = value; }
    public string LobbyName { get => lobbyName; set => lobbyName = value; }

    [SerializeField] private LobbyVisibility lobbyVisibility;
    [SerializeField] private string lobbyName;

    public OnlineSettings Clone()
    {
        return new OnlineSettings()
        {
            LobbyVisibility = this.LobbyVisibility,
            LobbyName = this.LobbyName
        };
    }

    public OnlineSettings Deserialize(NetPacket packet)
    {
        return new OnlineSettings()
        {
            LobbyVisibility = (LobbyVisibility)packet.ReadByte(),
            LobbyName = packet.ReadString()
        };
    }

    public void Serialize(NetPacket packet)
    {
        packet.Write((byte)LobbyVisibility);
        packet.Write(LobbyName);
    }
}

[Serializable]
public class GameSettings : INetSerializable<GameSettings>, IDeepClone<GameSettings>
{
    // Add game-specific settings here

    public GameSettings Clone()
    {
        // Clone game-specific settings here
        return new GameSettings() { };
    }

    public GameSettings Deserialize(NetPacket packet)
    {
        // Deserialize game-specific settings here
        return new GameSettings() { };
    }

    public void Serialize(NetPacket packet)
    {
        // Serialize game-specific settings here
    }
}

public enum LobbyVisibility
{
    Public,
    Private
}