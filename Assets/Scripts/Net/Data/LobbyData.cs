using System;
using System.Collections.Generic;

public class LobbyData
{
    public int LobbyId { get; set; }
    public List<UserData> LobbyUsers { get; set; } = new List<UserData>();
    public int UserCount { get { return LobbyUsers.Count; } }
    public UserData HostUser { get { return LobbyUsers.Count > 0 ? LobbyUsers[0] : null; } }
    public LobbySettings Settings { get; set; } = new LobbySettings();
}

public class LobbySettings
{
#if CNS_TRANSPORT_STEAMWORKS
    public ulong SteamCode { get; set; }
#endif
    public int MaxUsers { get; set; } = 256;
    public LobbyVisibility LobbyVisibility { get; set; } = LobbyVisibility.PRIVATE;
    public string LobbyName { get; set; } = "Default Lobby";
}