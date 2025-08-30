using UnityEngine;

public class ChatServerService : ServerService
{
    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.CHAT_MESSAGE:
                {
                    byte playerId = packet.ReadByte();
                    UserData thisUser = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    if (thisUser != null && thisUser.PlayerId == user.PlayerId)
                    {
                        string message = packet.ReadString();
                        lobby.SendToGame(PacketBuilder.ChatMessage(thisUser, message), TransportMethod.Reliable);
                    }
                    break;
                }
        }
    }

    public override void Tick(ServerLobby lobby)
    {
        // Nothing
    }

    public override void UserJoined(ServerLobby lobby, UserData user)
    {

    }

    public override void UserJoinedGame(ServerLobby lobby, UserData user)
    {
        lobby.SendToGame(PacketBuilder.ChatUserJoined(user), TransportMethod.Reliable);
    }

    public override void UserLeft(ServerLobby lobby, UserData user)
    {
        lobby.SendToGame(PacketBuilder.ChatUserLeft(user), TransportMethod.Reliable);
    }
}
