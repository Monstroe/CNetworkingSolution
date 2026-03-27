using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEngine;

public class ObjectServerService : ServerService
{
    public delegate void ObjectSpawnedEventHandler(ServerObject obj);
    public event ObjectSpawnedEventHandler OnObjectSpawned;

    public delegate void ObjectDestroyedEventHandler(ServerObject obj);
    public event ObjectDestroyedEventHandler OnObjectDestroyed;

    public Dictionary<ushort, ServerObject> ServerObjects { get; private set; } = new Dictionary<ushort, ServerObject>();
    public Dictionary<ushort, ServerTransform> ServerTransforms { get; private set; } = new Dictionary<ushort, ServerTransform>();

    public RpcBus RpcBus { get; private set; } = new RpcBus();
    public EventBus EventBus { get; private set; } = new EventBus();
    public NetMap Map { get; private set; }

    [Tooltip("The map prefab to be instantiated on the server.")]
    [SerializeField] private NetMap mapPrefab;

    private bool startingObjectsInitialized = false;
    private List<ushort> spawnedStartingObjectIds = new List<ushort>();
    private List<ushort> destroyedStartingObjectIds = new List<ushort>();

    // The object server service is special because it handles all networked object communication
    // Server services should run first (with the exception of the game and lobby service), then server objects
    public override void Init(ServerLobby lobby)
    {
        base.Init(lobby);
        // Init Map
        Map = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity).GetComponent<NetMap>();
        Map.transform.SetParent(this.transform);
        foreach (Renderer r in Map.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = false;
        }
        foreach (ClientObject obj in Map.GetComponentsInChildren<ClientObject>(true))
        {
            obj.gameObject.TryGetComponent(out Collider objCollider);
            if (objCollider != null)
            {
                objCollider.enabled = false;
            }
            obj.enabled = false;
        }
    }

    public override void ReceiveData(UserData user, NetPacket packet, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    CommandType objectCommand = (CommandType)packet.ReadByte();
                    ServerObjects.TryGetValue(objectId, out ServerObject serverObject);
                    if (serverObject != null)
                    {
                        serverObject.ReceiveData(user, packet, objectCommand, transportMethod);
                    }
                    break;
                }
            case CommandType.OBJECT_SPAWN_REQUEST:
                {
                    int clientPrefabKey = packet.ReadInt();
                    Vector3 position = packet.ReadVector3();
                    Quaternion rotation = packet.ReadQuaternion();
                    SpawnObject(user, clientPrefabKey, position, rotation, transportMethod, false, false);
                    break;
                }
            case CommandType.OBJECT_DESTROY_REQUEST:
                {
                    ushort objectId = packet.ReadUShort();
                    if (ServerObjects.TryGetValue(objectId, out ServerObject serverObject) && user.PlayerId == serverObject.OwnerId)
                    {
                        DestroyObject(serverObject);
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
                    ServerObjects.TryGetValue(objectId, out ServerObject serverObject);
                    if (serverObject != null)
                    {
                        serverObject.ReceiveDataUnconnected(ipEndPoint, packet, objectCommand);
                    }
                    break;
                }
        }
    }

    public override void Tick()
    {
        foreach (var serverObject in ServerObjects.Values)
        {
            serverObject.Tick();
        }
    }

    public override void UserJoined(UserData joinedUser)
    {
        foreach (var serverObject in ServerObjects.Values)
        {
            serverObject.UserJoined(joinedUser);
        }
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        if (!startingObjectsInitialized)
        {
            foreach (ClientObject clientObj in Map.GetStartingClientObjects())
            {
                SpawnObject(joinedUser, clientObj.PrefabKey, clientObj.transform.position, clientObj.transform.rotation, null, true, false);
            }
            startingObjectsInitialized = true;
        }

        // Initialize starting objects (AKA objects already placed on the map) for the joining user
        lobby.SendToUser(joinedUser, ObjectsInit(spawnedStartingObjectIds.ToArray()), TransportMethod.Reliable);
        // Destroy any starting objects that have already been destroyed by other players
        foreach (ushort destroyedObjectId in destroyedStartingObjectIds)
        {
            lobby.SendToUser(joinedUser, ObjectDestroy(destroyedObjectId), TransportMethod.Reliable);
        }

        // Spawn the rest of the objects for the joining user (not starting objects and not player objects)
        // Spawning happens first in the Server Service
        foreach (ServerObject obj in ServerObjects.Values.Where(o => !spawnedStartingObjectIds.Contains(o.Id)))// && o.Id >= byte.MaxValue))
        {
            Tuple<int, string> clientPrefabInfo = NetResources.Instance.GetClientPrefabFromServerKey(obj.PrefabKey);
            if (clientPrefabInfo != null)
            {
                lobby.SendToUser(joinedUser, ObjectSpawn(obj.Id, clientPrefabInfo.Item1, obj.transform.position, obj.transform.rotation, obj.Id <= byte.MaxValue, obj.OwnerId), TransportMethod.Reliable);
            }
        }

        // Handle UserJoinedGame for all existing objects
        foreach (var serverObject in ServerObjects.Values)
        {
            serverObject.UserJoinedGame(joinedUser);
        }
    }

    public override void UserLeft(UserData leftUser)
    {
        foreach (var serverObject in ServerObjects.Values)
        {
            serverObject.UserLeft(leftUser);
        }
    }

    public void SpawnObject(UserData spawningUser, int clientPrefabKey, Vector3 position, Quaternion rotation, TransportMethod? transportMethod, bool isStartingObject, bool setThisPlayerAsOwner)
    {
        Tuple<int, string> serverPrefabInfo = NetResources.Instance.GetServerPrefabFromClientKey(clientPrefabKey);
        if (serverPrefabInfo != null)
        {
            ServerObject serverObj = InstantiateOnServer(serverPrefabInfo.Item2, position, rotation, setThisPlayerAsOwner ? spawningUser.PlayerId : null, false);
            ushort id = lobby.GenerateObjectId();

            if (isStartingObject)
            {
                spawnedStartingObjectIds.Add(id);
            }
            else
            {
                lobby.SendToGame(ObjectSpawn(id, clientPrefabKey, serverObj.transform.position, serverObj.transform.rotation, false, serverObj.OwnerId), transportMethod ?? TransportMethod.Reliable);
            }

            serverObj.Init(id, lobby);
            OnObjectSpawned?.Invoke(serverObj);
        }
    }

    public void DestroyObject(ServerObject serverObj)
    {
        if (spawnedStartingObjectIds.Contains(serverObj.Id))
        {
            destroyedStartingObjectIds.Add(serverObj.Id);
        }

        OnObjectDestroyed?.Invoke(serverObj);
        DestroyOnServer(serverObj, true);
    }

    /* PACKETS */

    public enum ObjectCommandType
    {
        OBJECT_SPAWN_REQUEST,
        OBJECT_DESTROY_REQUEST,
        OBJECT_COMMUNICATION,
        OBJECT_SPAWN,
        OBJECT_DESTROY,
        OBJECT_TRANSFORM,
        OBJECT_RPC,
        OBJECTS_INIT
    }

    public static NetPacket ObjectCommunication(INetObject netObject, NetPacket packet)
    {
        packet.Insert(0, NetResources.GenerateServiceId<ObjectClientService>());
        packet.Insert(1, (byte)ObjectCommandType.OBJECT_COMMUNICATION);
        packet.Insert(2, netObject.Id);
        return packet;
    }

    public static NetPacket ObjectsInit(ushort[] startingObjectIds)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<ObjectClientService>());
        packet.Write((byte)ObjectCommandType.OBJECTS_INIT);
        packet.Write(startingObjectIds);
        return packet;
    }

    public static NetPacket ObjectSpawn(ushort objectId, int clientPrefabKey, Vector3 pos, Quaternion rot, bool isPlayer, byte? ownerId)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<ObjectClientService>());
        packet.Write((byte)ObjectCommandType.OBJECT_SPAWN);
        packet.Write(objectId);
        packet.Write(clientPrefabKey);
        packet.Write(pos);
        packet.Write(rot);
        packet.Write(isPlayer);
        if (ownerId != null)
            packet.Write(ownerId.Value);
        return packet;
    }

    public static NetPacket ObjectDestroy(ushort objectId)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<ObjectClientService>());
        packet.Write((byte)ObjectCommandType.OBJECT_DESTROY);
        packet.Write(objectId);
        return packet;
    }

    public static NetPacket ObjectTransform(Vector3 position, Quaternion rotation)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<ObjectClientService>());
        packet.Write((byte)ObjectCommandType.OBJECT_TRANSFORM);
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
        packet.Write(NetResources.GenerateServiceId<ObjectClientService>());
        packet.Write((byte)ObjectCommandType.OBJECT_RPC);
        packet.Write(methodId);

        for (int i = 0; i < args.Length; i++)
        {
            packet.Write(args[i], parameters[i].ParameterType);
        }
        return packet;
    }
}
