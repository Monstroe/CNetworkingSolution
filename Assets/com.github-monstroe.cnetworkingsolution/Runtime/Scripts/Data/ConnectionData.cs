using System;

public class ConnectionData : INetSerializable<ConnectionData>
{
#if CNS_SERVER_MULTIPLE
    public Guid TokenId { get; set; }
    public Guid UserGuid { get; set; }
#endif
    public int LobbyId { get; set; }
    public LobbyConnectionType LobbyConnectionType { get; set; }

    public ConnectionData Deserialize(NetPacket packet)
    {
        return new ConnectionData()
        {
#if CNS_SERVER_MULTIPLE
            TokenId = Guid.Parse(packet.ReadString()),
            UserGuid = Guid.Parse(packet.ReadString()),
#endif
            LobbyId = packet.ReadInt(),
            LobbyConnectionType = (LobbyConnectionType)packet.ReadByte()
        };
    }

    public void Serialize(NetPacket packet)
    {
#if CNS_SERVER_MULTIPLE
        packet.Write(TokenId.ToString());
        packet.Write(UserGuid.ToString());
#endif
        packet.Write(LobbyId);
        packet.Write((byte)LobbyConnectionType);
    }
}

public enum LobbyConnectionType
{
    Create,
    Join,
}
