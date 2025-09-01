using System;

public class ConnectionData : INetSerializable<ConnectionData>
{
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
    public Guid TokenId { get; set; }
#endif
    public int LobbyId { get; set; }
    public Guid UserGuid { get; set; }
    public UserSettings UserSettings { get; set; }
    public LobbySettings LobbySettings { get; set; }

    public ConnectionData Deserialize(ref NetPacket packet)
    {
        return new ConnectionData()
        {
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
            TokenId = Guid.Parse(packet.ReadString()),
#endif
            LobbyId = packet.ReadInt(),
            UserGuid = Guid.Parse(packet.ReadString()),
            UserSettings = new UserSettings().Deserialize(ref packet),
            LobbySettings = new LobbySettings().Deserialize(ref packet)
        };
    }

    public void Serialize(ref NetPacket packet)
    {
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        packet.Write(TokenId.ToString());
#endif
        packet.Write(LobbyId);
        packet.Write(UserGuid.ToString());
        UserSettings.Serialize(ref packet);
        LobbySettings.Serialize(ref packet);
    }
}