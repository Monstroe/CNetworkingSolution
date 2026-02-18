using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class EntityServerService : ServerService
{
    public Dictionary<ushort, ServerEntity> ServerEntities { get; private set; } = new Dictionary<ushort, ServerEntity>();

    public override void ReceiveData(UserData user, NetPacket packet, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, CommandType commandType)
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
