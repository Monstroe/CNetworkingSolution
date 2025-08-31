using UnityEngine;

public class FXServerService : ServerService
{
    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.SFX:
                lobby.SendToGame(packet, transportMethod ?? TransportMethod.Reliable, user);
                break;

            case CommandType.VFX:
                lobby.SendToGame(packet, transportMethod ?? TransportMethod.Reliable, user);
                break;
        }
    }

    public override void Tick(ServerLobby lobby)
    {
        // Nothing
    }

    public override void UserJoined(ServerLobby lobby, UserData joinedUser)
    {
        // Nothing
    }

    public override void UserJoinedGame(ServerLobby lobby, UserData joinedUser)
    {
        // Nothing
    }

    public override void UserLeft(ServerLobby lobby, UserData leftUser)
    {
        // Nothing
    }
}
