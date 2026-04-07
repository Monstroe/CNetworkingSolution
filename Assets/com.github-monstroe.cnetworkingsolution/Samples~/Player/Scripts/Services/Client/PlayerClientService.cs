using System.Collections.Generic;

[ServiceId("PlayerService")]
public class PlayerClientService : ClientService
{
    public Dictionary<UserData, ClientPlayer> ClientPlayers { get; private set; } = new Dictionary<UserData, ClientPlayer>();
}
