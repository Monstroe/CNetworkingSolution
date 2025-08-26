using System;

public class ConnectionData
{
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
    public Guid TokenId { get; set; }
#endif
    public int LobbyId { get; set; }
    public Guid UserGuid { get; set; }
    public UserSettings UserSettings { get; set; }
    public LobbySettings LobbySettings { get; set; }
}