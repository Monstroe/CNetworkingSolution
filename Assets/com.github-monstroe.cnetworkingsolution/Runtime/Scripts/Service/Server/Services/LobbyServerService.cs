using System.Linq;

namespace CNetworkingSolution
{
    [ServiceId("LobbyService")]
    public class LobbyServerService : ServerService
    {
        public override void UserJoined(UserData joinedUser)
        {
            base.UserJoined(joinedUser);
            LobbyInitRpc(joinedUser, lobby.ServerTick, joinedUser.UserId, lobby.LobbyData.LobbyUsers.ToArray());
            LobbyUserJoinedRpc(joinedUser);
        }

        public override void LateUserLeft(UserData leftUser)
        {
            base.LateUserLeft(leftUser);
            LobbyUserLeftRpc(leftUser, leftUser.UserId);
        }

        [ClientRpc]
        private async void LobbyInitRpc([RpcSender] UserData joinedUser, ulong tick, ulong userId, UserData[] users)
        {
            LobbyInitializedEvent evt = new LobbyInitializedEvent()
            {
                JoinedUser = joinedUser,
                Tick = tick,
                LobbyUsers = users
            };
            var result = await lobby.TriggerGameEvent(evt);
            if (!result.Canceled)
            {
                InvokeOnUserClientService(joinedUser, nameof(LobbyInitRpc), tick, userId, users);
            }
        }

        [ClientRpc]
        private async void LobbyUserJoinedRpc(UserData joinedUser)
        {
            LobbyUserJoinedEvent evt = new LobbyUserJoinedEvent()
            {
                JoinedUser = joinedUser
            };
            var result = await lobby.TriggerGameEvent(evt);
            if (!result.Canceled)
            {
                InvokeOnLobbyClientServices(nameof(LobbyUserJoinedRpc), exception: joinedUser, joinedUser);
            }
        }

        [ClientRpc]
        private async void LobbyUserLeftRpc([RpcSender] UserData leftUser, ulong userId)
        {
            LobbyUserLeftEvent evt = new LobbyUserLeftEvent()
            {
                LeftUser = leftUser
            };
            var result = await lobby.TriggerGameEvent(evt);
            if (!result.Canceled)
            {
                InvokeOnLobbyClientServices(nameof(LobbyUserLeftRpc), exception: leftUser, userId);
            }
        }
    }

    public class LobbyInitializedEvent : GameEvent
    {
        public UserData JoinedUser { get; set; }
        public ulong Tick { get; set; }
        public UserData[] LobbyUsers { get; set; }
    }

    public class LobbyUserJoinedEvent : GameEvent
    {
        public UserData JoinedUser { get; set; }
    }

    public class LobbyUserLeftEvent : GameEvent
    {
        public UserData LeftUser { get; set; }
    }
}
