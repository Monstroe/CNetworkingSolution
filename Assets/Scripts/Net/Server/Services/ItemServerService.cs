using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemServerService : ServerService
{
    private bool startingItemsInitialized = false;
    private List<ushort> startingItemIds = new List<ushort>();

    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
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
        if (joinedUser.IsHost(lobby.LobbyData))
        {
            for (int i = 0; i < lobby.Map.GetComponentsInChildren<ClientObject>(true).Length; i++)
            {
                ushort newId = lobby.GenerateObjectId();
                ServerItem serverItem = new ServerItem(newId);
                lobby.GameData.ServerObjects.Add(newId, serverItem);
                lobby.GameData.ServerItems.Add(newId, serverItem);
                startingItemIds.Add(newId);
            }

            startingItemsInitialized = true;
        }

        if (startingItemsInitialized)
        {
            lobby.SendToUser(joinedUser, PacketBuilder.StartingItemsInit(startingItemIds.ToArray()), TransportMethod.Reliable);
        }

        // Spawning happens first in the Server Service
        foreach (ServerItem item in lobby.GameData.ServerItems.Values.Where(i => !startingItemIds.Contains(i.Id)))
        {
            lobby.SendToUser(joinedUser, PacketBuilder.ItemSpawn(item.Id, item.Type), TransportMethod.Reliable);
        }
    }

    public override void UserLeft(ServerLobby lobby, UserData leftUser)
    {
        // Nothing
    }
}
