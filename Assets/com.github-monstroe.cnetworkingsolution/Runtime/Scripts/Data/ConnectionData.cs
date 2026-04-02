public class ConnectionData : INetSerializable<ConnectionData>
{
    public int LobbyId { get; set; } = -1;
    public LobbyConnectionType LobbyConnectionType { get; internal set; }
    public NetPacket RequestPacket { get; internal set; }

    internal ConnectionData() { }

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
