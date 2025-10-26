using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ObjectClientService : ClientService
{
    void Start()
    {
        ClientManager.Instance.CurrentLobby.RegisterService(ServiceType.OBJECT, this);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    ServiceType objectServiceType = (ServiceType)packet.ReadByte();
                    CommandType objectCommand = (CommandType)packet.ReadByte();
                    ClientManager.Instance.CurrentLobby.GameData.ClientObjects.TryGetValue(objectId, out ClientObject clientObject);
                    if (clientObject != null)
                    {
                        clientObject.ReceiveData(packet, objectServiceType, objectCommand, transportMethod);
                    }
                    break;
                }
            case CommandType.OBJECTS_INIT:
                {
                    ushort[] startingObjectIds = packet.ReadUShorts();
                    List<ClientObject> startingClientObjects = GameContent.Instance.Map.GetStartingClientObjects();
                    for (int i = 0; i < startingClientObjects.Count; i++)
                    {
                        ClientObject obj = startingClientObjects[i];
                        obj.Init(startingObjectIds[i]);
                    }
                    break;
                }
            case CommandType.OBJECT_SPAWN:
                {
                    ushort objectId = packet.ReadUShort();
                    int prefabKey = packet.ReadInt();
                    Vector3 pos = packet.ReadVector3();
                    Quaternion rot = packet.ReadQuaternion();
                    byte? ownerId = packet.UnreadLength > 0 ? (byte?)packet.ReadByte() : null;

                    if (!ClientManager.Instance.CurrentLobby.GameData.ClientObjects.ContainsKey(objectId))
                    {
                        string prefabName = NetResources.Instance.GetClientPrefabPathFromKey(prefabKey);
                        if (string.IsNullOrEmpty(prefabName))
                        {
                            Debug.LogError("ObjectClientService ReceiveData could not find client prefab path for key: " + prefabKey);
                            return;
                        }

                        var handle = Addressables.LoadAssetAsync<GameObject>(prefabName).WaitForCompletion();
                        ClientObject obj = Instantiate(handle, pos, rot).GetComponent<ClientObject>();
                        if (ownerId != null)
                        {
                            UserData ownerUser = ClientManager.Instance.CurrentLobby.LobbyData.GameUsers.Where(user => user.PlayerId == ownerId.Value).FirstOrDefault();
                            if (ownerUser != null)
                            {
                                obj.Owner = ClientManager.Instance.CurrentLobby.GameData.ClientPlayers[ownerUser];
                            }
                            else
                            {
                                Debug.LogWarning($"ObjectClientService ReceiveData could not find owner user with PlayerId {ownerId.Value}");
                            }
                        }
                        obj.Init(objectId);
                    }
                    else
                    {
                        Debug.LogWarning($"Object with Id {objectId} already exists. Spawn request ignored.");
                    }
                    break;
                }
            case CommandType.OBJECT_DESTROY:
                {
                    ushort objectId = packet.ReadUShort();
                    if (ClientManager.Instance.CurrentLobby.GameData.ClientObjects.TryGetValue(objectId, out ClientObject obj))
                    {
                        obj.Remove();
                        Destroy(obj.gameObject);
                    }
                    else
                    {
                        Debug.LogWarning($"No object with Id {objectId} found. Destroy request ignored.");
                    }
                    break;
                }
        }
    }
}
