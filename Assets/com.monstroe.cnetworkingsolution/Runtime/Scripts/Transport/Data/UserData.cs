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

        internal UserData() { }

        public bool IsHost(LobbyData lobby)
        {
            return lobby.HostUser != null && ReferenceEquals(lobby.HostUser, this);
        }

        public int GetIndexInLobby(LobbyData lobby)
        {
            for (int i = 0; i < lobby.LobbyUsers.Count; i++)
            {
                if (ReferenceEquals(lobby.LobbyUsers[i], this))
                {
                    return i;
                }
            }

            return -1;
        }

        public static UserData Deserialize(NetPacket packet)
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

        public void DeserializeInto(NetPacket packet)
        {
            GlobalGuid = Guid.Parse(packet.ReadString());
            UserId = packet.ReadULong();
            PlayerId = packet.ReadByte();
            LobbyId = packet.ReadInt();
            InLobby = packet.ReadBool();
            InGame = packet.ReadBool();
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
