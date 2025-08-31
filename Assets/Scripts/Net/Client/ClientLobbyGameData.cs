
using System.Collections.Generic;
using UnityEngine;

public class ClientLobbyGameData
{
    public Dictionary<ushort, ClientObject> ClientObjects { get; private set; } = new Dictionary<ushort, ClientObject>();
    public Dictionary<UserData, OtherPlayer> OtherPlayers { get; private set; } = new Dictionary<UserData, OtherPlayer>();
    public Dictionary<ushort, ClientItem> ClientItems { get; private set; } = new Dictionary<ushort, ClientItem>();
}