using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public class ObjectServerService : ServerService
{
    public delegate void ObjectSpawnedEventHandler(ServerObject obj);
    public event ObjectSpawnedEventHandler OnObjectSpawned;

    public delegate void ObjectDestroyedEventHandler(ServerObject obj);
    public event ObjectDestroyedEventHandler OnObjectDestroyed;

    public Dictionary<ushort, ServerObject> ServerObjects { get; private set; } = new Dictionary<ushort, ServerObject>();
    public Dictionary<ushort, ServerTransform> ServerTransforms { get; private set; } = new Dictionary<ushort, ServerTransform>();

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

    public override void ReceiveData(UserData user, NetPacket packet, ushort commandType, TransportMethod? transportMethod)
    {
        base.ReceiveData(user, packet, commandType, transportMethod);
        switch ((ObjectCommandType)commandType)
        {
            case ObjectCommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    ushort objectCommand = packet.ReadUShort();
                    ServerObjects.TryGetValue(objectId, out ServerObject serverObject);
                    if (serverObject != null)
                    {
                        serverObject.ReceiveData(user, packet, objectCommand, transportMethod);
                    }
                    break;
                }
            case ObjectCommandType.OBJECT_SPAWN_REQUEST:
                {
                    ulong clientPrefabKey = packet.ReadULong();
                    Vector3 position = packet.ReadVector3();
                    Quaternion rotation = packet.ReadQuaternion();
                    SpawnObject(user, clientPrefabKey, position, rotation, transportMethod, false, false);
                    break;
                }
            case ObjectCommandType.OBJECT_DESTROY_REQUEST:
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

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ushort commandType)
    {
        base.ReceiveDataUnconnected(ipEndPoint, packet, commandType);
        switch ((ObjectCommandType)commandType)
        {
            case ObjectCommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    ushort objectCommand = packet.ReadUShort();
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
        base.Tick();
        foreach (var serverObject in ServerObjects.Values)
        {
            serverObject.Tick();
        }
    }

    public override void UserJoined(UserData joinedUser)
    {
        base.UserJoined(joinedUser);
        foreach (var serverObject in ServerObjects.Values)
        {
            serverObject.UserJoined(joinedUser);
        }
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        base.UserJoinedGame(joinedUser);
        if (!startingObjectsInitialized)
        {
            foreach (ClientObject clientObj in Map.GetStartingClientObjects())
            {
                SpawnObject(joinedUser, clientObj.PrefabKey, clientObj.transform.position, clientObj.transform.rotation, null, true, false);
            }
            startingObjectsInitialized = true;
        }

        // Initialize starting objects (AKA objects already placed on the map) for the joining user
        lobby.SendToUser(joinedUser, ObjectPacketBuilder.ObjectsInit(spawnedStartingObjectIds.ToArray()), TransportMethod.Reliable);
        // Destroy any starting objects that have already been destroyed by other players
        foreach (ushort destroyedObjectId in destroyedStartingObjectIds)
        {
            lobby.SendToUser(joinedUser, ObjectPacketBuilder.ObjectDestroy(destroyedObjectId), TransportMethod.Reliable);
        }

        // Spawn the rest of the objects for the joining user (not starting objects and not player objects)
        // Spawning happens first in the Server Service
        foreach (ServerObject obj in ServerObjects.Values.Where(o => !spawnedStartingObjectIds.Contains(o.Id)))// && o.Id >= byte.MaxValue))
        {
            Tuple<ulong, string> clientPrefabInfo = NetResources.Instance.GetClientPrefabFromServerKey(obj.PrefabKey);
            if (clientPrefabInfo != null)
            {
                lobby.SendToUser(joinedUser, ObjectPacketBuilder.ObjectSpawn(obj.Id, clientPrefabInfo.Item1, obj.transform.position, obj.transform.rotation, obj.Id <= byte.MaxValue, obj.OwnerId), TransportMethod.Reliable);
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
        base.UserLeft(leftUser);
        foreach (var serverObject in ServerObjects.Values)
        {
            serverObject.UserLeft(leftUser);
        }
    }

    internal void SpawnObject(UserData spawningUser, ulong clientPrefabKey, Vector3 position, Quaternion rotation, TransportMethod? transportMethod, bool isStartingObject, bool setThisPlayerAsOwner)
    {
        Tuple<ulong, string> serverPrefabInfo = NetResources.Instance.GetServerPrefabFromClientKey(clientPrefabKey);
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
                lobby.SendToGame(ObjectPacketBuilder.ObjectSpawn(id, clientPrefabKey, serverObj.transform.position, serverObj.transform.rotation, false, serverObj.OwnerId), transportMethod ?? TransportMethod.Reliable);
            }

            serverObj.Init(id, lobby);
            OnObjectSpawned?.Invoke(serverObj);
        }
    }

    internal void DestroyObject(ServerObject serverObj)
    {
        if (spawnedStartingObjectIds.Contains(serverObj.Id))
        {
            destroyedStartingObjectIds.Add(serverObj.Id);
        }

        OnObjectDestroyed?.Invoke(serverObj);
        DestroyOnServer(serverObj, true);
    }
}
