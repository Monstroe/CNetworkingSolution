using UnityEngine;

public class ChatServerService : ServerService
{
    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        throw new System.NotImplementedException();
    }

    public override void Tick(ServerLobby lobby)
    {
        throw new System.NotImplementedException();
    }

    public override void UserJoined(ServerLobby lobby, UserData user)
    {
        throw new System.NotImplementedException();
    }

    public override void UserLeft(ServerLobby lobby, UserData user)
    {
        throw new System.NotImplementedException();
    }
}
