
using System.Collections.Generic;

public class ServerLobbyGameData
{
    public Dictionary<UserData, ServerPlayer> ServerPlayers { get; private set; } = new Dictionary<UserData, ServerPlayer>();
    public Dictionary<ushort, ServerObject> ServerObjects { get; private set; } = new Dictionary<ushort, ServerObject>();
}