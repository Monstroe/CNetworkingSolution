using System;

namespace Monstroe.CNetworkingSolution
{
    public class UserData : INetSerializable<UserData>
    {
        public Guid GlobalGuid { get; internal set; }
        public ulong UserId { get; internal set; }
        public byte PlayerId { get; internal set; }
        public int LobbyId { get; internal set; } = -1;
        public bool InLobby { get; internal set; } = false;
        public bool InGame { get; internal set; } = false;

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
                InLobby = packet.ReadBool(),
                InGame = packet.ReadBool()
            };
        }

        public void Serialize(NetPacket packet)
        {
            packet.Write(GlobalGuid.ToString());
            packet.Write(UserId);
            packet.Write(PlayerId);
            packet.Write(LobbyId);
            packet.Write(InLobby);
            packet.Write(InGame);
        }
    }
}
