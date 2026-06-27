using System.Linq;
using UnityEngine;

namespace Monstroe.CNetworkingSolution
{
    [ServiceId("GameService")]
    public class GameClientService : ClientService
    {
        public delegate void GameInitializedEventHandler();
        public event GameInitializedEventHandler OnGameInitialized;

        public delegate void GameUserJoinedEventHandler(UserData user);
        public event GameUserJoinedEventHandler OnGameUserJoined;

        public delegate void GameUserLeftEventHandler(UserData user);
        public event GameUserLeftEventHandler OnGameUserLeft;

        public void JoinGame()
        {
            if (lobby.CurrentUser.InGame)
            {
                Debug.LogWarning("Current user is already marked as in-game.");
                return;
            }

            InvokeOnServerService(nameof(GameUserJoinedRpc), lobby.CurrentUser.PlayerId);
        }

        public void LeaveGame()
        {
            if (!lobby.CurrentUser.InGame)
            {
                Debug.LogWarning("Current user is not marked as in-game.");
                return;
            }

            InvokeOnServerService(nameof(GameUserLeftRpc), lobby.CurrentUser.PlayerId);
        }

        [ClientRpc]
        private void GameUserJoinedRpc(byte playerId)
        {
            UserData user = lobby.LobbyData.LobbyUsers.FirstOrDefault(u => u.PlayerId == playerId);
            if (user == null)
            {
                Debug.LogWarning($"Received GameUserJoinedRpc for player ID {playerId}, but no such user was found in the lobby.");
                return;
            }

            user.InGame = true;
            if (lobby.CurrentUser.PlayerId == playerId)
            {
                OnGameInitialized?.Invoke();
            }
            else
            {
                OnGameUserJoined?.Invoke(user);
            }
        }

        [ClientRpc]
        private void GameUserLeftRpc(byte playerId)
        {
            UserData user = lobby.LobbyData.LobbyUsers.FirstOrDefault(u => u.PlayerId == playerId);
            if (user == null)
            {
                Debug.LogWarning($"Received GameUserLeftRpc for player ID {playerId}, but no such user was found in the lobby.");
                return;
            }

            user.InGame = false;
            OnGameUserLeft?.Invoke(user);
        }
    }
}
