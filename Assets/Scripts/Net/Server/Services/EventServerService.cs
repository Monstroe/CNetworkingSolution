using UnityEngine;

public class EventServerService : ServerService
{
    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.EVENT_GROUND_HIT:
                {
                    byte playerId = packet.ReadByte();
                    UserData thisUser = lobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    if (thisUser != null && thisUser.PlayerId == user.PlayerId)
                    {
                        GroundHitArgs args = new GroundHitArgs().Deserialize(packet);
                        // Send these args to any other places where they should be modified here...
                        // Then, invoke the event with the modified args
                        lobby.EventManager.OnGroundHit.Invoke(args);
                        // This can be moved to a separate method (subscribed to this event) for better organization, but I'm sending an SFX as an example
                        lobby.SendToGame(PacketBuilder.PlaySFX("Collide", 1f, args.position), TransportMethod.Reliable);
                    }
                    break;
                }
                // ADD MORE EVENTS HERE
        }
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
