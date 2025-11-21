using System.Collections.Generic;
using UnityEngine;

public class InteractableServerService : ServerService
{
    public Dictionary<ushort, ServerInteractable> ServerInteractables { get; private set; } = new Dictionary<ushort, ServerInteractable>();

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
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
