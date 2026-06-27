using System.Collections.Generic;

namespace Monstroe.CNetworkingSolution
{
    public class LobbyData : INetSerializable<LobbyData>
    {
        public int LobbyId { get; internal set; } = -1;
        private readonly List<UserData> lobbyUsers = new List<UserData>();
        public IReadOnlyList<UserData> LobbyUsers => lobbyUsers;
        public IReadOnlyList<UserData> GameUsers => lobbyUsers.FindAll(u => u.InGame);
        public int UserCount => lobbyUsers.Count;
        public UserData HostUser => lobbyUsers.Count > 0 ? LobbyUsers[0] : null;

        internal LobbyData() { }

        internal void AddUser(UserData user)
        {
            lobbyUsers.Add(user);
        }

        internal void RemoveUser(UserData user)
        {
            lobbyUsers.Remove(user);
        }

        public LobbyData Deserialize(NetPacket packet)
        {
            LobbyData lobbyData = new LobbyData()
            {
                LobbyId = packet.ReadInt()
            };
            int userCount = packet.ReadByte();
            for (int i = 0; i < userCount; i++)
            {
                lobbyData.lobbyUsers.Add(new UserData().Deserialize(packet));
            }
            return lobbyData;
        }

        public void Serialize(NetPacket packet)
        {
            packet.Write(LobbyId);
            packet.Write((byte)LobbyUsers.Count);
            foreach (var user in LobbyUsers)
            {
                user.Serialize(packet);
            }
        }
    }
}
