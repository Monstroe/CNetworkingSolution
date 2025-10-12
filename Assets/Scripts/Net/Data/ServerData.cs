using System;
using System.Collections.Generic;

public class ServerData
{
    public Dictionary<ulong, UserData> ConnectedUsers { get; private set; } = new Dictionary<ulong, UserData>();
    public Dictionary<int, ServerLobby> ActiveLobbies { get; private set; } = new Dictionary<int, ServerLobby>();
#if CNS_SYNC_SERVER_MULTIPLE
    public ServerSettings Settings { get; set; } = new ServerSettings();
#endif
}

#if CNS_SYNC_SERVER_MULTIPLE
public class ServerSettings
{
    public Guid ServerId { get; set; }
    public string ServerKey { get; set; }
    public string ServerAddress { get; set; }
}
#endif