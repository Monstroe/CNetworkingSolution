using System.Linq;
using CNetworkingSolution;

[ServiceId("ChatService")]
public class ChatClientService : ClientService
{
    public delegate void ChatMessageReceivedEventHandler(UserData user, string message);
    public event ChatMessageReceivedEventHandler OnChatMessageReceived;

    public delegate void ChatUserJoinedEventHandler(UserData joinedUser, string welcomeMessage);
    public event ChatUserJoinedEventHandler OnChatUserJoined;

    public delegate void ChatUserLeftEventHandler(UserData leftUser, string farewellMessage);
    public event ChatUserLeftEventHandler OnChatUserLeft;

    public void SendChat(string message)
    {
        InvokeOnServerService(nameof(SendChatRpc), lobby.CurrentUser.PlayerId, message);
    }

    [ClientRpc]
    private void SendChatRpc(byte playerId, string message)
    {
        UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.FirstOrDefault(u => u.PlayerId == playerId);
        OnChatMessageReceived?.Invoke(user, message);
    }

    [ClientRpc]
    private void ChatUserJoinedRpc(byte playerId, string welcomeMessage)
    {
        UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.FirstOrDefault(u => u.PlayerId == playerId);
        OnChatUserJoined?.Invoke(user, welcomeMessage);
    }

    [ClientRpc]
    private void ChatUserLeftRpc(byte playerId, string farewellMessage)
    {
        UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.FirstOrDefault(u => u.PlayerId == playerId);
        OnChatUserLeft?.Invoke(user, farewellMessage);
    }
}
