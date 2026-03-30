using System;
using System.Collections.Generic;

public class ConnectionData : INetSerializable<ConnectionData>
{
    public int LobbyId { get; set; }
    public LobbyConnectionType LobbyConnectionType { get; internal set; }
    public byte[] Payload { get; set; }

    public void SetPayload<T>(T data) where T : INetSerializable
    {
        var packet = new NetPacket();
        data.Serialize(packet);
        Payload = packet.ByteArray;
    }

    public T GetPayload<T>() where T : INetSerializable<T>, new()
    {
        var packet = new NetPacket(Payload);
        return new T().Deserialize(packet);
    }

    public ConnectionData Deserialize(NetPacket packet)
    {
        ConnectionData connectionData = new ConnectionData()
        {
            LobbyId = packet.ReadInt(),
            LobbyConnectionType = (LobbyConnectionType)packet.ReadByte()
        };

        if (packet.UnreadLength > 0)
        {
            connectionData.Payload = packet.ReadBytes();
        }

        return connectionData;
    }

    public void Serialize(NetPacket packet)
    {
        packet.Write(LobbyId);
        packet.Write((byte)LobbyConnectionType);
        if (Payload != null)
        {
            packet.Write(Payload);
        }
    }
}

public enum LobbyConnectionType
{
    Create,
    JoinIfExists,
    JoinOrCreate
}
