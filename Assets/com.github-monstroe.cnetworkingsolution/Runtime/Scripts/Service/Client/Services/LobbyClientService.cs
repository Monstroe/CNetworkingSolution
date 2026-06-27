using System.Linq;
using UnityEngine;

namespace Monstroe.CNetworkingSolution
{
    [ServiceId("LobbyService")]
    public class LobbyClientService : ClientService
    {
        public delegate void LobbyInitializedEventHandler(ulong tick, LobbyData lobbyData);
        public event LobbyInitializedEventHandler OnLobbyInitialized;

        public delegate void LobbyUserJoinedEventHandler(UserData user);
        public event LobbyUserJoinedEventHandler OnLobbyUserJoined;

        public delegate void LobbyUserLeftEventHandler(UserData user);
        public event LobbyUserLeftEventHandler OnLobbyUserLeft;

        [ClientRpc]
        private void LobbyInitRpc(ulong tick, ulong userId, UserData[] users)
        {
            lobby.ClientTick = tick; // Update the client tick
            for (int i = 0; i < users.Length; i++)
            {
                lobby.LobbyData.AddUser(users[i]);
            }

            lobby.CurrentUser = lobby.LobbyData.LobbyUsers.FirstOrDefault(u => u.UserId == userId); // Set the local user data
            if (lobby.CurrentUser == null)
            {
                Debug.LogWarning($"Received LobbyInitRpc for user ID {userId}, but no such user was found in the lobby.");
                return;
            }

            OnLobbyInitialized?.Invoke(tick, lobby.LobbyData);
        }

        [ClientRpc]
        private void LobbyUserJoinedRpc(UserData user)
        {
            lobby.LobbyData.AddUser(user);
            OnLobbyUserJoined?.Invoke(user);
        }

        [ClientRpc]
        private void LobbyUserLeftRpc(ulong userId)
        {
            UserData user = lobby.LobbyData.LobbyUsers.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                Debug.LogWarning($"Received LobbyUserLeftRpc for user ID {userId}, but no such user was found in the lobby.");
                return;
            }

            lobby.LobbyData.RemoveUser(user);
            OnLobbyUserLeft?.Invoke(user);
        }
    }
}