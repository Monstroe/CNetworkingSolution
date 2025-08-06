using System.Collections.Generic;

public class LobbyData
{
    public int LobbyId { get; set; }
    public List<UserData> LobbyUsers { get; set; } = new List<UserData>();
    public int UserCount { get { return LobbyUsers.Count; } }
    public UserData HostUser { get { return LobbyUsers.Count > 0 ? LobbyUsers[0] : null; } }
    public LobbySettings Settings { get; set; } = new LobbySettings();
}