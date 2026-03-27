using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
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

    public override void ReceiveData(NetPacket packet, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    CommandType objectCommand = (CommandType)packet.ReadByte();
                    ClientObjects.TryGetValue(objectId, out ClientObject clientObject);
                    if (clientObject != null)
                    {
                        clientObject.ReceiveData(packet, objectCommand, transportMethod);
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

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, CommandType commandType)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    CommandType objectCommand = (CommandType)packet.ReadByte();
                    ClientObjects.TryGetValue(objectId, out ClientObject clientObject);
                    if (clientObject != null)
                    {
                        clientObject.ReceiveDataUnconnected(ipEndPoint, packet, objectCommand);
                    }
                    break;
                }
        }
    }

    /* PACKETS */

    public static NetPacket ObjectCommunication(INetObject netObject, NetPacket packet)
    {
        packet.Insert(0, NetResources.GenerateServiceId<ObjectServerService>());
        packet.Insert(1, (byte)ObjectServerService.ObjectCommandType.OBJECT_COMMUNICATION);
        packet.Insert(2, netObject.Id);
        return packet;
    }

    public static NetPacket ObjectSpawnRequest(string clientPrefabPath, Vector3 pos, Quaternion rot)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<ObjectServerService>());
        packet.Write((byte)ObjectServerService.ObjectCommandType.OBJECT_SPAWN_REQUEST);
        int key = NetResources.Instance.GetClientPrefabKeyFromPath(clientPrefabPath);
        if (key == 0)
        {
            Debug.LogError("ObjectSpawnRequest could not find client prefab key for path: " + clientPrefabPath);
            return null;
        }
        packet.Write(key);
        packet.Write(pos);
        packet.Write(rot);
        return packet;
    }

    public static NetPacket ObjectDestroyRequest(ushort objectId)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<ObjectServerService>());
        packet.Write((byte)ObjectServerService.ObjectCommandType.OBJECT_DESTROY_REQUEST);
        packet.Write(objectId);
        return packet;
    }

    public static NetPacket ObjectTransform(Vector3 position, Quaternion rotation)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<ObjectServerService>());
        packet.Write((byte)ObjectServerService.ObjectCommandType.OBJECT_TRANSFORM);
        packet.Write(position);
        packet.Write(rotation);
        return packet;
    }

    public static NetPacket ObjectRpc(ulong methodId, MethodInfo method, params object[] args)
    {
        var parameters = method.GetParameters();
        if (args.Length != parameters.Length)
        {
            throw new ArgumentException("RPC argument count mismatch");
        }

        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<ObjectServerService>());
        packet.Write((byte)ObjectServerService.ObjectCommandType.OBJECT_RPC);
        packet.Write(methodId);

        for (int i = 0; i < args.Length; i++)
        {
            packet.Write(args[i], parameters[i].ParameterType);
        }
        return packet;
    }
}
