using UnityEngine;

public class ClientChat : ClientObject
{
    public delegate void ChatMessageReceivedEventHandler(UserData user, string message);
    public event ChatMessageReceivedEventHandler OnChatMessageReceived;

    public override void Init(ushort id, ClientLobby lobby)
    {
        base.Init(id, lobby);
        lobby.GetService<ChatClientService>().Chat = this;
    }

    public void SendChat(string message)
    {
        ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.ChatMessage(ClientManager.Instance.CurrentLobby.CurrentUser, message), TransportMethod.Reliable);
        InvokeOnServerObject(nameof(SendChatRpc), ClientManager.Instance.CurrentLobby.CurrentUser, message);
    }

    [Rpc]
    private void SendChatRpc(byte playerId, string message)
    {
        UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
        Debug.Log($"Chat message from {user.Settings.UserName}: {message}");
        OnChatMessageReceived?.Invoke(user, message);
    }
}
