
using System.Collections.Generic;
using UnityEngine;

public class ClientLobbyGameData
{
    public Dictionary<UserData, OtherPlayer> OtherPlayers { get; private set; } = new Dictionary<UserData, OtherPlayer>();
    public Dictionary<ushort, ClientObject> ClientObjects { get; private set; } = new Dictionary<ushort, ClientObject>();
}