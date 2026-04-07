using UnityEngine;
using CNetworkingSolution;

[ServiceId("ChatService")]
public class ChatServerService : ServerService
{
    public override void UserJoinedGame(UserData joinedUser)
    {
        base.UserJoinedGame(joinedUser);
        ChatUserJoinedEvent joinEvent = new ChatUserJoinedEvent()
        {
            JoinedUser = joinedUser,
            WelcomeMessage = $"Player {joinedUser.PlayerId} has joined the game."
        };

        var result = lobby.TriggerGameEvent(joinEvent).Result;
        if (!result.Canceled)
        {
            ChatUserJoinedRpc(joinEvent.JoinedUser.PlayerId, joinEvent.WelcomeMessage);
        }
    }

    public override void UserLeftGame(UserData leftUser)
    {
        base.UserLeftGame(leftUser);
        ChatUserLeftEvent leftEvent = new ChatUserLeftEvent()
        {
            LeftUser = leftUser,
            FarewellMessage = $"Player {leftUser.PlayerId} has left the game."
        };

        var result = lobby.TriggerGameEvent(leftEvent).Result;
        if (!result.Canceled)
        {
            ChatUserLeftRpc(leftEvent.LeftUser.PlayerId, leftEvent.FarewellMessage);
        }
    }

    [ServerRpc]
    private async void SendChatRpc([RpcSender] UserData user, byte playerId, string message)
    {
        if (user.PlayerId != playerId)
        {
            Debug.LogWarning($"Received SendChatRpc with player ID {playerId}, but the sender's player ID is {user.PlayerId}.");
            return;
        }

        ChatMessageReceivedEvent chatEvent = new ChatMessageReceivedEvent()
        {
            User = user,
            Message = message
        };
        var result = await lobby.TriggerGameEvent(chatEvent);
        if (!result.Canceled)
        {
            InvokeOnGameClientServices(nameof(SendChatRpc), chatEvent.User.PlayerId, chatEvent.Message);
        }
    }

    [ClientRpc]
    private void ChatUserJoinedRpc(byte playerId, string welcomeMessage)
    {
        InvokeOnGameClientServices(nameof(ChatUserJoinedRpc), playerId, welcomeMessage);
    }

    [ClientRpc]
    private void ChatUserLeftRpc(byte playerId, string farewellMessage)
    {
        InvokeOnGameClientServices(nameof(ChatUserLeftRpc), playerId, farewellMessage);
    }
}

public class ChatMessageReceivedEvent : GameEvent
{
    public UserData User { get; set; }
    public string Message { get; set; }
}

public class ChatUserJoinedEvent : GameEvent
{
    public UserData JoinedUser { get; set; }
    public string WelcomeMessage { get; set; }
}

public class ChatUserLeftEvent : GameEvent
{
    public UserData LeftUser { get; set; }
    public string FarewellMessage { get; set; }
}
