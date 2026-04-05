using System.Linq;
using UnityEngine;

[ServiceId("ChatService")]
public class ChatServerService : ServerService
{
    [Rpc]
    private async void SendChatRpc(byte playerId, string message)
    {
        UserData user = lobby.LobbyData.LobbyUsers.FirstOrDefault(u => u.PlayerId == playerId);
        if (user == null)
        {
            Debug.LogWarning($"User with PlayerId {playerId} not found in lobby.");
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

    [Rpc]
    private void ChatUserJoinedRpc(byte playerId, string welcomeMessage)
    {
        InvokeOnGameClientServices(nameof(ChatUserJoinedRpc), playerId, welcomeMessage);
    }

    [Rpc]
    private void ChatUserLeftRpc(byte playerId, string farewellMessage)
    {
        InvokeOnGameClientServices(nameof(ChatUserLeftRpc), playerId, farewellMessage);
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
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

    public override void UserLeft(UserData leftUser)
    {
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
