using System;
using System.Collections.Generic;
using System.Linq;

public class ServerData
{
    public Guid ServerId { get; set; }
    public string SecretKey { get; set; }
    public Dictionary<ulong, UserData> ConnectedUsers { get; private set; } = new Dictionary<ulong, UserData>();
    public Dictionary<int, ServerLobby> ActiveLobbies { get; private set; } = new Dictionary<int, ServerLobby>();

#if CNS_LOBBY_SINGLE
    public ServerLobby CurrentLobby { get => ActiveLobbies.Count == 1 ? ActiveLobbies.Values.First() : null; }
#endif

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
    public TransportSettings Settings { get; set; } = new TransportSettings();
#endif
}
