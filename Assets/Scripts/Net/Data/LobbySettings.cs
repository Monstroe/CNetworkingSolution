public class LobbySettings
{
    public ulong InternalCode { get; set; }
    public int MaxUsers { get; set; } = 256;
    public LobbyVisibility LobbyVisibility { get; set; } = LobbyVisibility.PRIVATE;
    public string LobbyName { get; set; } = "Default Lobby";
}
