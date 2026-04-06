[ServiceId("ChatService")]
public class ChatServerService : ServerService
{
    [ServerRpc]
    private async void SendChatRpc([RpcSender] UserData user, string message)
    {
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

    [ServerRpc]
    private void ChatUserJoinedRpc([RpcSender] UserData user, string welcomeMessage)
    {
        InvokeOnGameClientServices(nameof(ChatUserJoinedRpc), user.PlayerId, welcomeMessage);
    }

    [ServerRpc]
    private void ChatUserLeftRpc([RpcSender] UserData user, string farewellMessage)
    {
        InvokeOnGameClientServices(nameof(ChatUserLeftRpc), user.PlayerId, farewellMessage);
    }

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
            ChatUserJoinedRpc(joinEvent.JoinedUser, joinEvent.WelcomeMessage);
        }
    }

    public override void UserLeft(UserData leftUser)
    {
        base.UserLeft(leftUser);
        ChatUserLeftEvent leftEvent = new ChatUserLeftEvent()
        {
            LeftUser = leftUser,
            FarewellMessage = $"Player {leftUser.PlayerId} has left the game."
        };

        var result = lobby.TriggerGameEvent(leftEvent).Result;
        if (!result.Canceled)
        {
            ChatUserLeftRpc(leftEvent.LeftUser, leftEvent.FarewellMessage);
        }
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
