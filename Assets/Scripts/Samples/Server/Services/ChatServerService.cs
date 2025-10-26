using UnityEngine;

public class ChatServerService : ServerService
{
    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.CHAT_MESSAGE:
                {
                    byte playerId = packet.ReadByte();
                    UserData thisUser = lobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    if (thisUser != null && thisUser.PlayerId == user.PlayerId)
                    {
                        string message = packet.ReadString();
                        lobby.SendToGame(PacketBuilder.ChatMessage(user, message), TransportMethod.Reliable);
                    }
                    break;
                }
        }
    }

    public override void Tick()
    {
        // Nothing
    }

    public override void UserJoined(UserData joinedUser)
    {
        //Nothing
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        lobby.SendToGame(PacketBuilder.ChatUserJoined(joinedUser), TransportMethod.Reliable);
    }

    public override void UserLeft(UserData leftUser)
    {
        lobby.SendToGame(PacketBuilder.ChatUserLeft(leftUser), TransportMethod.Reliable);
    }
}
