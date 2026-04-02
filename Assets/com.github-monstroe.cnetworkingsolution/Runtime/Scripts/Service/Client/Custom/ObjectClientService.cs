using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.AddressableAssets;

[ServiceId("ObjectService")]
public class ObjectClientService : ClientService
{
    public delegate void ObjectSpawnedEventHandler(ClientObject obj);
    public event ObjectSpawnedEventHandler OnObjectSpawned;

    public delegate void ObjectDestroyedEventHandler(ClientObject obj);
    public event ObjectDestroyedEventHandler OnObjectDestroyed;

    public Dictionary<ushort, ClientObject> ClientObjects { get; private set; } = new Dictionary<ushort, ClientObject>();
    public Dictionary<ushort, ClientTransform> ClientTransforms { get; private set; } = new Dictionary<ushort, ClientTransform>();

    public NetMap Map { get => mapInstance; private set => mapInstance = value; }

    [Tooltip("The current instance of the map on the client.")]
    [SerializeField] private NetMap mapInstance;

    public void SetMapInstance(NetMap mapObj)
    {
        mapInstance = mapObj;
    }

    public override void ReceiveData(NetPacket packet, ushort commandType, TransportMethod? transportMethod)
    {
        switch ((ObjectCommandType)commandType)
        {
            case ObjectCommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    ushort objectCommand = packet.ReadUShort();
                    ClientObjects.TryGetValue(objectId, out ClientObject clientObject);
                    if (clientObject != null)
                    {
                        clientObject.ReceiveData(packet, objectCommand, transportMethod);
                    }
                    break;
                }
            case ObjectCommandType.OBJECTS_INIT:
                {
                    ushort[] startingObjectIds = packet.ReadUShorts();
                    List<ClientObject> startingClientObjects = Map.GetStartingClientObjects();
                    for (int i = 0; i < startingClientObjects.Count; i++)
                    {
                        ClientObject obj = startingClientObjects[i];
                        obj.Init(startingObjectIds[i], lobby);
                    }
                    break;
                }
            case ObjectCommandType.OBJECT_SPAWN:
                {
                    ushort objectId = packet.ReadUShort();
                    ulong prefabKey = packet.ReadULong();
                    Vector3 pos = packet.ReadVector3();
                    Quaternion rot = packet.ReadQuaternion();
                    bool isPlayer = packet.ReadBool();
                    byte? ownerId = packet.UnreadLength > 0 ? (byte?)packet.ReadByte() : null;

                    if (!ClientObjects.ContainsKey(objectId))
                    {
                        string prefabName = NetResources.Instance.GetClientPrefabPathFromKey(prefabKey);
                        if (string.IsNullOrEmpty(prefabName))
                        {
                            Debug.LogError("ObjectClientService ReceiveData could not find client prefab path for key: " + prefabKey);
                            return;
                        }

                        var handle = Addressables.LoadAssetAsync<GameObject>(prefabName).WaitForCompletion();
                        ClientObject obj = Instantiate(handle, pos, rot).GetComponent<ClientObject>();
                        if (ownerId.HasValue)
                        {
                            obj.SetOwner(ownerId.Value);
                            obj.SetAsPlayer(isPlayer);
                        }
                        obj.Init(objectId, lobby);
                        OnObjectSpawned?.Invoke(obj);
                    }
                    else
                    {
                        Debug.LogWarning($"Object with Id {objectId} already exists. Spawn request ignored.");
                    }
                    break;
                }
            case ObjectCommandType.OBJECT_DESTROY:
                {
                    ushort objectId = packet.ReadUShort();
                    if (ClientObjects.TryGetValue(objectId, out ClientObject obj))
                    {
                        OnObjectDestroyed?.Invoke(obj);
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

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ushort commandType)
    {
        switch ((ObjectCommandType)commandType)
        {
            case ObjectCommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    ushort objectCommand = packet.ReadUShort();
                    ClientObjects.TryGetValue(objectId, out ClientObject clientObject);
                    if (clientObject != null)
                    {
                        clientObject.ReceiveDataUnconnected(ipEndPoint, packet, objectCommand);
                    }
                    break;
                }
        }
    }
}
