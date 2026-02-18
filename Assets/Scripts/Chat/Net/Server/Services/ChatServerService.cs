using System.Net;
using UnityEngine;

public class ChatServerService : ServerService
{
    public ServerChat Chat { get; private set; }

    public void SetChat(ServerChat chat)
    {
        Chat = chat;
    }

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
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
