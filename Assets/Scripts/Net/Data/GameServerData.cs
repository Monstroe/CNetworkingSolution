using System;
using System.Collections.Generic;

public class GameServerData
{
    public Guid GameServerId { get; set; }
    public string GameServerKey { get; set; }
    public string GameServerAddress { get; set; }
    public Dictionary<uint, UserData> ConnectedUsers { get; private set; } = new Dictionary<uint, UserData>();
    public Dictionary<int, ServerLobby> ActiveLobbies { get; private set; } = new Dictionary<int, ServerLobby>();
}