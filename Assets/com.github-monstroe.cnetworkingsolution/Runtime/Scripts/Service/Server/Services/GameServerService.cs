using UnityEngine;

[ServiceId("GameService")]
public class GameServerService : ServerService
{
    public override void UserJoinedGame(UserData joinedUser)
    {
        base.UserJoinedGame(joinedUser);
        InvokeOnGameClientServices(nameof(GameUserJoinedRpc), joinedUser.PlayerId);
    }

    public override void UserLeftGame(UserData leftUser)
    {
        base.UserLeftGame(leftUser);
        InvokeOnGameClientServices(nameof(GameUserLeftRpc), leftUser.PlayerId);
    }

    [ServerRpc]
    private async void GameUserJoinedRpc([RpcSender] UserData joinedUser)
    {
        if (joinedUser.InGame)
        {
            Debug.LogWarning($"User {joinedUser.UserId} tried to join the game, but they are already marked as in-game.");
            return;
        }

        joinedUser.InGame = true;
        GameUserJoinedEvent evt = new GameUserJoinedEvent()
        {
            JoinedUser = joinedUser
        };
        var result = await lobby.TriggerGameEvent(evt);
        if (!result.Canceled)
        {
            lobby.UserJoinedGame(joinedUser);
        }
    }

    [ServerRpc]
    private async void GameUserLeftRpc([RpcSender] UserData leftUser)
    {
        if (!leftUser.InGame)
        {
            Debug.LogWarning($"User {leftUser.UserId} tried to leave the game, but they are not marked as in-game.");
            return;
        }

        leftUser.InGame = false;
        GameUserLeftEvent evt = new GameUserLeftEvent()
        {
            LeftUser = leftUser
        };
        var result = await lobby.TriggerGameEvent(evt);
        if (!result.Canceled)
        {
            lobby.UserLeftGame(leftUser);
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
