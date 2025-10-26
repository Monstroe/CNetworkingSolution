
using System.Collections.Generic;
using UnityEngine;

public class ClientGameData
{
    public Dictionary<UserData, ClientPlayer> ClientPlayers { get; private set; } = new Dictionary<UserData, ClientPlayer>();
    public Dictionary<ushort, ClientInteractable> ClientInteractables { get; private set; } = new Dictionary<ushort, ClientInteractable>();

    // Remember, EVERYTHING is a ClientObject
    public Dictionary<ushort, ClientObject> ClientObjects { get; private set; } = new Dictionary<ushort, ClientObject>();
}