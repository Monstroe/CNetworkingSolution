using System.Collections.Generic;
using UnityEngine;

public class MapServerService : ServerService
{
    private bool hostPlayerJoined = false;
    private List<ushort> startingObjectIds = new List<ushort>();

    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void Tick(ServerLobby lobby)
    {
        // Nothing
    }

    public override void UserJoined(ServerLobby lobby, UserData user)
    {
        // Nothing
    }

    public override void UserJoinedGame(ServerLobby lobby, UserData user)
    {
        if (!hostPlayerJoined && user.UserId == lobby.LobbyData.HostUser.UserId)
        {
            hostPlayerJoined = true;
            foreach (ClientObject obj in lobby.Map.GetStartingClientObjects())
            {
                ushort newId = lobby.GenerateObjectId();

                // Create Server Object (ADD NEW CLIENT OBJECT TYPES HERE, MAKE SURE YOUR CHILD CLASSES ARE AT THE TOP)
                switch (obj)
                {
                    case ClientInteractable:
                        {
                            ServerInteractable interactable = new ServerInteractable(newId);
                            lobby.GameData.ServerObjects.Add(newId, interactable);
                            break;
                        }
                    default:
                        Debug.LogWarning($"<color=red><b>CNS</b></color>: Object {obj.name} is not a valid ClientObject and will not be added to the server object list.");
                        break;
                }

                startingObjectIds.Add(newId);
            }
        }

        if (hostPlayerJoined)
        {
            lobby.SendToUser(user, PacketBuilder.MapObjectsInit(startingObjectIds.ToArray()), TransportMethod.Reliable);
        }
    }

    public override void UserLeft(ServerLobby lobby, UserData user)
    {
        // Nothing
    }
}
