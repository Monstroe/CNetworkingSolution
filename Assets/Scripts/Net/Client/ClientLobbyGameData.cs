
using System.Collections.Generic;
using UnityEngine;

public class ClientLobbyGameData
{
    public Dictionary<ushort, ClientObject> ClientObjects { get; private set; } = new Dictionary<ushort, ClientObject>();
    public Dictionary<UserData, ClientPlayer> ClientPlayers { get; private set; } = new Dictionary<UserData, ClientPlayer>();
    public Dictionary<ushort, ClientItem> ClientItems { get; private set; } = new Dictionary<ushort, ClientItem>();
}