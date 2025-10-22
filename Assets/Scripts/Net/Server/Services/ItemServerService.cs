using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemServerService : ServerService
{
    private bool startingItemsInitialized = false;
    private List<ushort> spawnedStartingItemIds = new List<ushort>();
    private List<ushort> destroyedStartingItemIds = new List<ushort>();

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.ITEM_SPAWN:
                {
                    ItemType itemType = (ItemType)packet.ReadByte();
                    Vector3 position = packet.ReadVector3();
                    Quaternion rotation = packet.ReadQuaternion();
                    SpawnItem(itemType, position, rotation, lobby, transportMethod);
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
                        DestroyItem(serverItem, lobby, transportMethod);
                    }
                    break;
                }
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
        if (!startingItemsInitialized)
        {
            foreach (ClientItem obj in lobby.Map.GetComponentsInChildren<ClientItem>(true))
            {
                SpawnItem(obj.Type, obj.transform.position, obj.transform.rotation, lobby, null, true);
            }

            startingItemsInitialized = true;
        }

        // Initialize starting items (AKA items already placed on map prefab) for the joining user
        lobby.SendToUser(joinedUser, PacketBuilder.ItemsInit(spawnedStartingItemIds.ToArray()), TransportMethod.Reliable);
        // Destroy any starting items that have already been destroyed by other players
        foreach (ushort destroyedItemId in destroyedStartingItemIds)
        {
            lobby.SendToUser(joinedUser, PacketBuilder.ItemDestroy(destroyedItemId), TransportMethod.Reliable);
        }

        // Spawn the rest of the items for the joining user
        // Spawning happens first in the Server Service
        foreach (ServerItem item in lobby.GameData.ServerItems.Values.Where(i => !spawnedStartingItemIds.Contains(i.Id)))
        {
            lobby.SendToUser(joinedUser, PacketBuilder.ItemSpawn(item.Id, item.Type, item.Position, item.Rotation), TransportMethod.Reliable);
        }
    }

    public override void UserLeft(UserData leftUser)
    {
        // Nothing
    }

    public void SpawnItem(ItemType itemType, Vector3 position, Quaternion rotation, ServerLobby lobby, TransportMethod? transportMethod, bool isStartingItem = false)
    {
        ushort newId = lobby.GenerateObjectId();
        ServerItem serverItem = null; //new ServerItem(newId, lobby);
        serverItem.Type = itemType;
        serverItem.Position = position;
        serverItem.Rotation = rotation;
        lobby.GameData.ServerObjects.Add(newId, serverItem);
        lobby.GameData.ServerItems.Add(newId, serverItem);

        if (isStartingItem)
        {
            spawnedStartingItemIds.Add(newId);
        }
        else
        {
            lobby.SendToGame(PacketBuilder.ItemSpawn(newId, itemType, position, rotation), transportMethod ?? TransportMethod.Reliable);
        }
    }

    public void DestroyItem(ServerItem item, ServerLobby lobby, TransportMethod? transportMethod)
    {
        if (spawnedStartingItemIds.Contains(item.Id))
        {
            destroyedStartingItemIds.Add(item.Id);
        }

        if (item.InteractingPlayer != null)
        {
            item.Drop(item.InteractingPlayer, lobby, item.InteractingPlayer.User, null, transportMethod);
        }
        lobby.GameData.ServerObjects.Remove(item.Id);
        lobby.GameData.ServerItems.Remove(item.Id);
        lobby.SendToGame(PacketBuilder.ItemDestroy(item.Id), transportMethod ?? TransportMethod.Reliable);
    }
}
