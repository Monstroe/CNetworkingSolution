using System;

public class ConnectionData : INetSerializable<ConnectionData>
{
#if CNS_SYNC_SERVER_MULTIPLE
    public Guid TokenId { get; set; }
#endif
    public int LobbyId { get; set; }
    public LobbyConnectionType LobbyConnectionType { get; set; }
    public Guid UserGuid { get; set; }
    public UserSettings UserSettings { get; set; }
    public LobbySettings LobbySettings { get; set; }

    public ConnectionData Deserialize(NetPacket packet)
    {
        return new ConnectionData()
        {
#if CNS_SYNC_SERVER_MULTIPLE
            TokenId = Guid.Parse(packet.ReadString()),
#endif
            LobbyId = packet.ReadInt(),
            LobbyConnectionType = (LobbyConnectionType)packet.ReadByte(),
            UserGuid = Guid.Parse(packet.ReadString()),
            UserSettings = new UserSettings().Deserialize(packet),
            LobbySettings = new LobbySettings().Deserialize(packet)
        };
    }

    public void Serialize(NetPacket packet)
    {
#if CNS_SYNC_SERVER_MULTIPLE
        packet.Write(TokenId.ToString());
#endif
        packet.Write(LobbyId);
        packet.Write((byte)LobbyConnectionType);
        packet.Write(UserGuid.ToString());
        UserSettings.Serialize(packet);
        LobbySettings.Serialize(packet);
    }
}

public enum LobbyConnectionType
{
    CREATE,
    JOIN,
}

public enum LobbyRejectionType
{
    LOBBY_FULL,
    LOBBY_NOT_FOUND,
}