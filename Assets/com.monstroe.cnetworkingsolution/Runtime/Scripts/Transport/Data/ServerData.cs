using System;
using System.Collections.Generic;

namespace Monstroe.CNetworkingSolution
{
    public class ServerData
    {
        public Guid ServerId { get; internal set; }
        public string SecretKey { get; internal set; }
        private readonly Dictionary<ulong, ConnectionRequestedEventResult> connectingUsers = new Dictionary<ulong, ConnectionRequestedEventResult>();
        internal IReadOnlyDictionary<ulong, ConnectionRequestedEventResult> ConnectingUsers => connectingUsers;
        private readonly Dictionary<ulong, UserData> connectedUsers = new Dictionary<ulong, UserData>();
        public IReadOnlyDictionary<ulong, UserData> ConnectedUsers => connectedUsers;
        private readonly Dictionary<int, ServerLobby> activeLobbies = new Dictionary<int, ServerLobby>();
        public IReadOnlyDictionary<int, ServerLobby> ActiveLobbies => activeLobbies;

        internal ServerData() { }

        internal void AddConnectingUser(ConnectionRequestedEventResult result)
        {
            connectingUsers[result.ConnectingUser.UserId] = result;
        }

        internal bool RemoveConnectingUser(ulong userId)
        {
            return connectingUsers.Remove(userId);
        }

        internal void AddConnectedUser(UserData user)
        {
            connectedUsers[user.UserId] = user;
        }

        internal bool RemoveConnectedUser(ulong userId)
        {
            return connectedUsers.Remove(userId);
        }

        internal void AddLobby(ServerLobby lobby)
        {
            activeLobbies[lobby.LobbyData.LobbyId] = lobby;
        }

        internal bool RemoveLobby(int lobbyId)
        {
            return activeLobbies.Remove(lobbyId);
        }
    }
}
