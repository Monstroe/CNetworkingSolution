using System.Collections.Generic;
using UnityEngine;

public class ItemClientService : ClientService
{
    void Start()
    {
        ClientManager.Instance.CurrentLobby.RegisterService(ServiceType.ITEM, this);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.ITEMS_INIT:
                {
                    ushort[] startingItemIds = packet.ReadUShorts();
                    List<ClientItem> startingClientItems = GameContent.Instance.Map.GetStartingClientItems();
                    for (int i = 0; i < startingClientItems.Count; i++)
                    {
                        ClientItem obj = startingClientItems[i];
                        obj.Init(startingItemIds[i]);
                        ClientManager.Instance.CurrentLobby.GameData.ClientObjects.Add(obj.Id, obj);
                        ClientManager.Instance.CurrentLobby.GameData.ClientItems.Add(obj.Id, obj);
                    }
                    break;
                }
            case CommandType.ITEM_SPAWN:
                {
                    ushort itemId = packet.ReadUShort();
                    ItemType itemType = (ItemType)packet.ReadByte();
                    Vector3 pos = packet.ReadVector3();
                    Quaternion rot = packet.ReadQuaternion();

                    if (!ClientManager.Instance.CurrentLobby.GameData.ClientItems.ContainsKey(itemId) || !ClientManager.Instance.CurrentLobby.GameData.ClientObjects.ContainsKey(itemId))
                    {
                        ClientItem item = Instantiate(Resources.Load<GameObject>($"Prefabs/Items/{itemType}Item"), pos, rot).GetComponent<ClientItem>();
                        item.Init(itemId);
                        item.Type = itemType;
                        ClientManager.Instance.CurrentLobby.GameData.ClientObjects.Add(item.Id, item);
                        ClientManager.Instance.CurrentLobby.GameData.ClientItems.Add(item.Id, item);
                    }
                    else
                    {
                        Debug.LogWarning($"Item with Id {itemId} already exists. Spawn request ignored.");
                    }
                    break;
                }
            case CommandType.ITEM_DESTROY:
                {
                    ushort itemId = packet.ReadUShort();
                    if (ClientManager.Instance.CurrentLobby.GameData.ClientItems.TryGetValue(itemId, out ClientItem item) && ClientManager.Instance.CurrentLobby.GameData.ClientObjects.ContainsKey(itemId))
                    {
                        if (item.InteractingPlayer != null)
                        {
                            item.Drop(item.InteractingPlayer, null, transportMethod);
                        }
                        ClientManager.Instance.CurrentLobby.GameData.ClientItems.Remove(item.Id);
                        ClientManager.Instance.CurrentLobby.GameData.ClientObjects.Remove(item.Id);
                        Destroy(item.gameObject);
                    }
                    else
                    {
                        Debug.LogWarning($"No item with Id {itemId} found. Destroy request ignored.");
                    }
                    break;
                }
        }
    }
}
