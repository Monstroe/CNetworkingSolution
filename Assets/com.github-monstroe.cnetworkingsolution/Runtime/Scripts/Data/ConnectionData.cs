public class ConnectionData : INetSerializable<ConnectionData>
{
    public int LobbyId { get; set; }
    public LobbyConnectionType LobbyConnectionType { get; private set; }
    public NetPacket RequestPacket { get; private set; }

    public ConnectionData Deserialize(NetPacket packet)
    {
        ConnectionData connectionData = new ConnectionData()
        {
            LobbyId = packet.ReadInt(),
            LobbyConnectionType = (LobbyConnectionType)packet.ReadByte()
        };

        if (packet.UnreadLength > 0)
        {
            connectionData.RequestPacket = new NetPacket(packet.ReadBytes());
        }

        return connectionData;
    }

    public void Serialize(NetPacket packet)
    {
        packet.Write(LobbyId);
        packet.Write((byte)LobbyConnectionType);
        if (RequestPacket != null)
        {
            packet.Write(RequestPacket.ByteArray);
        }
    }
}

public enum LobbyConnectionType
{
    Create,
    JoinIfExists,
    JoinOrCreate
}
