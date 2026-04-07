namespace CNetworkingSolution
{
    public class ConnectionData : INetSerializable<ConnectionData>
    {
        public int LobbyId { get; set; } = -1;
        public LobbyConnectionType LobbyConnectionType { get; set; }
        public NetPacket RequestPacket { get; set; }

        public ConnectionData Deserialize(NetPacket packet)
        {
            ConnectionData connectionData = new ConnectionData()
            {
                LobbyId = packet.ReadInt(),
                LobbyConnectionType = (LobbyConnectionType)packet.ReadByte()
            };

            if (packet.UnreadLength > sizeof(int))
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
}
