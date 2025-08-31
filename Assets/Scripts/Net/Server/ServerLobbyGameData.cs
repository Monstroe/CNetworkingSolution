
using System.Collections.Generic;

public class ServerLobbyGameData
{
    public Dictionary<ushort, ServerObject> ServerObjects { get; private set; } = new Dictionary<ushort, ServerObject>();
    public Dictionary<UserData, ServerPlayer> ServerPlayers { get; private set; } = new Dictionary<UserData, ServerPlayer>();
    public Dictionary<ushort, ServerItem> ServerItems { get; private set; } = new Dictionary<ushort, ServerItem>();
}