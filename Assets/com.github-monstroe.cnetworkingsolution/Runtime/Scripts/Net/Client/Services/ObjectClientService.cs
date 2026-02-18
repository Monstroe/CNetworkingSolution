using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ObjectClientService : ClientService
{
    public delegate void ObjectSpawnedEventHandler(ClientObject obj);
    public event ObjectSpawnedEventHandler OnObjectSpawned;

    public delegate void ObjectDestroyedEventHandler(ClientObject obj);
    public event ObjectDestroyedEventHandler OnObjectDestroyed;

    public Dictionary<ushort, ClientObject> ClientObjects { get; private set; } = new Dictionary<ushort, ClientObject>();
    public Dictionary<ushort, ClientTransform> ClientTransforms { get; private set; } = new Dictionary<ushort, ClientTransform>();

    public RpcBus RpcBus { get; private set; } = new RpcBus();
    public NetMap Map { get => mapInstance; private set => mapInstance = value; }

    [Tooltip("The current instance of the map on the client.")]
    [SerializeField] private NetMap mapInstance;

    public void SetMapInstance(NetMap mapObj)
    {
        mapInstance = mapObj;
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
                    ClientObjects.TryGetValue(objectId, out ClientObject clientObject);
                    if (clientObject != null)
                    {
                        clientObject.ReceiveData(packet, objectServiceType, objectCommand, transportMethod);
                    }
                    break;
                }
            case CommandType.OBJECTS_INIT:
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
            case CommandType.OBJECT_SPAWN:
                {
                    ushort objectId = packet.ReadUShort();
                    int prefabKey = packet.ReadInt();
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
            case CommandType.OBJECT_DESTROY:
                {
                    ushort objectId = packet.ReadUShort();
                    if (ClientObjects.TryGetValue(objectId, out ClientObject obj))
                    {
                        obj.Remove();
                        OnObjectDestroyed?.Invoke(obj);
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

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    ServiceType objectServiceType = (ServiceType)packet.ReadByte();
                    CommandType objectCommand = (CommandType)packet.ReadByte();
                    ClientObjects.TryGetValue(objectId, out ClientObject clientObject);
                    if (clientObject != null)
                    {
                        clientObject.ReceiveDataUnconnected(ipEndPoint, packet, objectServiceType, objectCommand);
                    }
                    break;
                }
        }
    }
}
