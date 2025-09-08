using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemServerService : ServerService
{
    private bool startingItemsInitialized = false;
    private List<ushort> startingItemIds = new List<ushort>();

    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.ITEM_SPAWN:
                {
                    ItemType itemType = (ItemType)packet.ReadByte();
                    Vector3 pos = packet.ReadVector3();
                    Quaternion rotation = packet.ReadQuaternion();
                    ushort newId = lobby.GenerateObjectId();
                    ServerItem serverItem = new ServerItem(newId);
                    serverItem.Type = itemType;
                    serverItem.Position = pos;
                    serverItem.Rotation = rotation;
                    lobby.GameData.ServerObjects.Add(newId, serverItem);
                    lobby.GameData.ServerItems.Add(newId, serverItem);
                    lobby.SendToGame(PacketBuilder.ItemSpawn(newId, itemType, pos, rotation), transportMethod ?? TransportMethod.Reliable);
                    break;
                }
            case CommandType.ITEM_DESTROY:
                {
                    ushort itemId = packet.ReadUShort();
                    if (lobby.GameData.ServerItems.TryGetValue(itemId, out ServerItem serverItem))
                    {
                        if (user.PlayerId != serverItem.InteractingPlayer?.Id)
                        {
                            Debug.LogWarning($"User {user.UserId} attempted to destroy item {itemId} they are not interacting with.");
                            return;
                        }

                        if (startingItemIds.Contains(serverItem.Id))
                            startingItemIds.Remove(serverItem.Id);
                        serverItem.Drop(serverItem.InteractingPlayer, lobby, user, null, transportMethod);
                        lobby.GameData.ServerObjects.Remove(serverItem.Id);
                        lobby.GameData.ServerItems.Remove(serverItem.Id);
                        lobby.SendToGame(PacketBuilder.ItemDestroy(serverItem.Id), transportMethod ?? TransportMethod.Reliable);
                    }
                    break;
                }
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
        if (!startingItemsInitialized)
        {
            foreach (ClientItem obj in lobby.Map.GetComponentsInChildren<ClientItem>(true))
            {
                ushort newId = lobby.GenerateObjectId();
                ServerItem serverItem = new ServerItem(newId);
                serverItem.Type = obj.Type;
                serverItem.Position = obj.transform.position;
                serverItem.Rotation = obj.transform.rotation;
                lobby.GameData.ServerObjects.Add(newId, serverItem);
                lobby.GameData.ServerItems.Add(newId, serverItem);
                startingItemIds.Add(newId);
            }

            startingItemsInitialized = true;
        }

        if (startingItemsInitialized)
        {
            lobby.SendToUser(joinedUser, PacketBuilder.ItemsInit(startingItemIds.ToArray()), TransportMethod.Reliable);
        }

        // Spawning happens first in the Server Service
        foreach (ServerItem item in lobby.GameData.ServerItems.Values.Where(i => !startingItemIds.Contains(i.Id)))
        {
            lobby.SendToUser(joinedUser, PacketBuilder.ItemSpawn(item.Id, item.Type, item.Position, item.Rotation), TransportMethod.Reliable);
        }
    }

    public override void UserLeft(ServerLobby lobby, UserData leftUser)
    {
        // Nothing
    }
}
