using UnityEngine;

namespace Monstroe.CNetworkingSolution
{
    [ServiceId("GameService")]
    public class GameServerService : ServerService
    {
        public override void UserJoinedGame(UserData joinedUser)
        {
            base.UserJoinedGame(joinedUser);
            InvokeOnGameClientServices(nameof(GameUserJoinedRpc), joinedUser.PlayerId);
        }

        public override void LateUserLeftGame(UserData leftUser)
        {
            base.LateUserLeftGame(leftUser);
            InvokeOnGameClientServices(nameof(GameUserLeftRpc), leftUser.PlayerId);
        }

        [ServerRpc]
        private async void GameUserJoinedRpc([RpcSender] UserData joinedUser, byte playerId)
        {
            if (joinedUser.InGame)
            {
                Debug.LogWarning($"User {joinedUser.UserId} tried to join the game, but they are already marked as in-game.");
                return;
            }

            if (joinedUser.PlayerId != playerId)
            {
                Debug.LogWarning($"Received GameUserJoinedRpc with player ID {playerId}, but the sender's player ID is {joinedUser.PlayerId}.");
                return;
            }

            GameUserJoinedEvent evt = new GameUserJoinedEvent()
            {
                JoinedUser = joinedUser
            };
            var result = await lobby.TriggerGameEvent(evt);
            if (!result.Canceled)
            {
                lobby.EarlyUserJoinedGame(joinedUser);
                lobby.UserJoinedGame(joinedUser);
                lobby.LateUserJoinedGame(joinedUser);
            }
        }

        [ServerRpc]
        private async void GameUserLeftRpc([RpcSender] UserData leftUser, byte playerId)
        {
            if (!leftUser.InGame)
            {
                Debug.LogWarning($"User {leftUser.UserId} tried to leave the game, but they are not marked as in-game.");
                return;
            }

            if (leftUser.PlayerId != playerId)
            {
                Debug.LogWarning($"Received GameUserLeftRpc with player ID {playerId}, but the sender's player ID is {leftUser.PlayerId}.");
                return;
            }

            GameUserLeftEvent evt = new GameUserLeftEvent()
            {
                LeftUser = leftUser
            };
            var result = await lobby.TriggerGameEvent(evt);
            if (!result.Canceled)
            {
                lobby.EarlyUserLeftGame(leftUser);
                lobby.UserLeftGame(leftUser);
                lobby.LateUserLeftGame(leftUser);
            }
        }
    }

    public class GameUserJoinedEvent : GameEvent
    {
        public UserData JoinedUser { get; set; }
    }

    public class GameUserLeftEvent : GameEvent
    {
        public UserData LeftUser { get; set; }
    }
}