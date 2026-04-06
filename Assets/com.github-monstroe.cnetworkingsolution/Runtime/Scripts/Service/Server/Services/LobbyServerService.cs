using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[ServiceId("LobbyService")]
public class LobbyServerService : ServerService
{
    public override void UserJoined(UserData joinedUser)
    {
        base.UserJoined(joinedUser);
        LobbyInitRpc(joinedUser, lobby.ServerTick, joinedUser.UserId, lobby.LobbyData.LobbyUsers.ToArray());
        LobbyUserJoinedRpc(joinedUser);
    }

    public override void UserLeft(UserData leftUser)
    {
        base.UserLeft(leftUser);
        LobbyUserLeftRpc(leftUser);
    }

    [ClientRpc]
    private async void LobbyInitRpc(UserData joinedUser, ulong tick, ulong userId, UserData[] users)
    {
        joinedUser.InLobby = true;
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
    private async void LobbyUserLeftRpc(UserData leftUser)
    {
        LobbyUserLeftEvent evt = new LobbyUserLeftEvent()
        {
            LeftUser = leftUser
        };
        var result = await lobby.TriggerGameEvent(evt);
        if (!result.Canceled)
        {
            InvokeOnLobbyClientServices(nameof(LobbyUserLeftRpc), exception: leftUser, leftUser.UserId);
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
