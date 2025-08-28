using System;

public class UserData
{
    public Guid GlobalGuid { get; set; }
    public ulong UserId { get; set; }
    public byte PlayerId { get; set; }
    public int LobbyId { get; set; } = -1;
    public bool InLobby { get { return LobbyId > -1; } }
    public UserSettings Settings { get; set; } = new UserSettings();
}

public class UserSettings
{
    public string UserName { get; set; }
}
