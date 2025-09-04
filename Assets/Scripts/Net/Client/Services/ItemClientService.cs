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
            case CommandType.STARTING_ITEMS_INIT:
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

                    if (!ClientManager.Instance.CurrentLobby.GameData.ClientItems.ContainsKey(itemId))
                    {
                        string itemTypeName = char.ToUpper(itemType.ToString()[0]) + itemType.ToString().Substring(1).ToLower();
                        ClientItem item = Instantiate(Resources.Load<GameObject>($"Prefabs/Items/{itemTypeName}")).GetComponent<ClientItem>();
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
        }
    }
}
