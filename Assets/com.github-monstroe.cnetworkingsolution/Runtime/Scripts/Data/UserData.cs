using System;

public class UserData : INetSerializable<UserData>
{
    public Guid GlobalGuid { get; set; }
    public ulong UserId { get; set; }
    public byte PlayerId { get; set; }
    public int LobbyId { get; set; } = -1;
    public bool InLobby { get { return LobbyId > -1; } }
    public bool InGame { get; set; } = false;

    public bool IsHost(LobbyData lobby)
    {
        return lobby.HostUser != null && lobby.HostUser.UserId == UserId;
    }

    public UserData Deserialize(NetPacket packet)
    {
        return new UserData()
        {
            GlobalGuid = Guid.Parse(packet.ReadString()),
            UserId = packet.ReadULong(),
            PlayerId = packet.ReadByte(),
            LobbyId = packet.ReadInt(),
            InGame = packet.ReadBool()
        };
    }

    public void Serialize(NetPacket packet)
    {
        packet.Write(GlobalGuid.ToString());
        packet.Write(UserId);
        packet.Write(PlayerId);
        packet.Write(LobbyId);
        packet.Write(InGame);
    }
}
