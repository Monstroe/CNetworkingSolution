using UnityEngine;

public class ObjectServerService : ServerService
{
    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    ServiceType objectServiceType = (ServiceType)packet.ReadByte();
                    CommandType objectCommand = (CommandType)packet.ReadByte();
                    lobby.GameData.ServerObjects.TryGetValue(objectId, out ServerObject serverObject);
                    serverObject?.ReceiveData(lobby, user, packet, objectServiceType, objectCommand, transportMethod);
                    break;
                }
        }
    }

    public override void Tick(ServerLobby lobby)
    {
        foreach (var serverObject in lobby.GameData.ServerObjects.Values)
        {
            serverObject.Tick(lobby);
        }
    }

    public override void UserJoined(ServerLobby lobby, UserData user)
    {
        // Nothing
    }

    public override void UserJoinedGame(ServerLobby lobby, UserData user)
    {
        // Nothing
    }

    public override void UserLeft(ServerLobby lobby, UserData user)
    {
        // Nothing
    }
}
