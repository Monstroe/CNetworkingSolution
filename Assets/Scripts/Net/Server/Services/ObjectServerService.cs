using UnityEngine;

public class ObjectServerService : ServerService
{
    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    ServiceType objectServiceType = (ServiceType)packet.ReadByte();
                    CommandType objectCommand = (CommandType)packet.ReadByte();
                    lobby.GameData.ServerObjects.TryGetValue(objectId, out ServerObject serverObject);
                    serverObject?.ReceiveData(user, packet, objectServiceType, objectCommand, transportMethod);
                    break;
                }
        }
    }

    public override void Tick()
    {
        foreach (var serverObject in lobby.GameData.ServerObjects.Values)
        {
            serverObject.Tick();
        }
    }

    public override void UserJoined(UserData joinedUser)
    {
        foreach (var serverObject in lobby.GameData.ServerObjects.Values)
        {
            serverObject.UserJoined(joinedUser);
        }
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        foreach (var serverObject in lobby.GameData.ServerObjects.Values)
        {
            serverObject.UserJoinedGame(joinedUser);
        }
    }

    public override void UserLeft(UserData leftUser)
    {
        foreach (var serverObject in lobby.GameData.ServerObjects.Values)
        {
            serverObject.UserLeft(leftUser);
        }
    }
}
