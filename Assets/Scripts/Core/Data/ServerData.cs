using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ServerData
{
    public Dictionary<ulong, UserData> ConnectedUsers { get; private set; } = new Dictionary<ulong, UserData>();
    public Dictionary<int, ServerLobby> ActiveLobbies { get; private set; } = new Dictionary<int, ServerLobby>();

#if CNS_LOBBY_SINGLE
    public ServerLobby CurrentLobby { get => ActiveLobbies.Count == 1 ? ActiveLobbies.Values.First() : null; }
#endif

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
    public ServerSettings Settings { get; set; } = new ServerSettings();
#endif
}

[Serializable]
public class ServerSettings
{
    public Guid ServerId { get; set; }
    public string ServerKey { get => serverKey; set => serverKey = value; }
    public string ServerAddress { get => serverAddress; set => serverAddress = value; }
    public ushort ServerPort { get => serverPort; set => serverPort = value; }

    [SerializeField] private string serverKey;
    [SerializeField] private string serverAddress;
    [SerializeField] private ushort serverPort;
}
