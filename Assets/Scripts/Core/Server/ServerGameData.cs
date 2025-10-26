
using System.Collections.Generic;

public class ServerGameData
{
    public Dictionary<UserData, ServerPlayer> ServerPlayers { get; private set; } = new Dictionary<UserData, ServerPlayer>();
    public Dictionary<ushort, ServerInteractable> ServerInteractables { get; private set; } = new Dictionary<ushort, ServerInteractable>();

    // Remember, EVERYTHING is a ServerObject
    public Dictionary<ushort, ServerObject> ServerObjects { get; private set; } = new Dictionary<ushort, ServerObject>();
}