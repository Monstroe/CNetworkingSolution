using System;
using System.Collections.Generic;

namespace Monstroe.CNetworkingSolution
{
    public class LobbyData : INetSerializable<LobbyData>
    {
        public delegate void UserAddedEventHandler(UserData user);
        public event UserAddedEventHandler OnUserAdded;
        public delegate void UserRemovedEventHandler(UserData user);
        public event UserRemovedEventHandler OnUserRemoved;

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
            OnUserAdded?.Invoke(user);
        }

        internal void RemoveUser(UserData user)
        {
            lobbyUsers.Remove(user);
            OnUserRemoved?.Invoke(user);
        }

        public static LobbyData Deserialize(NetPacket packet)
        {
            LobbyData lobbyData = new LobbyData()
            {
                LobbyId = packet.ReadInt()
            };
            int userCount = packet.ReadByte();
            for (int i = 0; i < userCount; i++)
            {
                lobbyData.lobbyUsers.Add(UserData.Deserialize(packet));
            }
            return lobbyData;
        }

        public void DeserializeInto(NetPacket packet)
        {
            LobbyId = packet.ReadInt();
            int userCount = packet.ReadByte();
            for (int i = 0; i < userCount; i++)
            {
                // If the user may already exist in the list
                if (lobbyUsers.Count > i)
                {
                    Guid incomingUserGuid = Guid.Parse(packet.ReadString(false));
                    // If the user ID matches
                    if (lobbyUsers[i].GlobalGuid == incomingUserGuid)
                    {
                        // Deserialize into the existing UserData instance
                        lobbyUsers[i].DeserializeInto(packet);
                    }
                    else // If the user ID does not match
                    {
                        // GlobalGuids never change, so this user has left the lobby and should be removed from the list
                        lobbyUsers.RemoveAt(i--);
                    }
                }
                else // If the user does not exist in the list
                {
                    // Create a new UserData instance and add it to the back of the list
                    UserData user = new UserData();
                    user.DeserializeInto(packet);
                    lobbyUsers.Add(user);
                }
            }
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
