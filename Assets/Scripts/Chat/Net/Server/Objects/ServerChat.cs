using UnityEngine;

public class ServerChat : ServerObject
{

    public override void Init(ushort id, ServerLobby lobby)
    {
        base.Init(id, lobby);
        lobby.GetService<ChatServerService>().Chat = this;
    }

    [Rpc]
    private void SendChatRpc(byte playerId, string message)
    {
        InvokeOnGameClientObjects(nameof(SendChatRpc), playerId, message);
    }

    public override void Tick()
    {
        // Nothing
    }

    public override void UserJoined(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserLeft(UserData leftUser)
    {
        // Nothing
    }
}
