using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public class ObjectServerService : ServerService
{
    public Dictionary<ushort, ServerObject> ServerObjects { get; private set; } = new Dictionary<ushort, ServerObject>();

    private List<ushort> spawnedStartingObjectIds = new List<ushort>();
    private List<ushort> destroyedStartingObjectIds = new List<ushort>();

    // The object server service is special because it handles all networked object communication
    // Server services should run first (with the exception of the lobby service), then server objects
    public override void Init(ServerLobby lobby)
    {
        base.Init(lobby);
    }

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    ServiceType objectServiceType = (ServiceType)packet.ReadByte();
                    CommandType objectCommand = (CommandType)packet.ReadByte();
                    ServerObjects.TryGetValue(objectId, out ServerObject serverObject);
                    if (serverObject != null)
                    {
                        serverObject.ReceiveData(user, packet, objectServiceType, objectCommand, transportMethod);
                    }
                    break;
                }
            case CommandType.OBJECT_SPAWN_REQUEST:
                {
                    int clientPrefabKey = packet.ReadInt();
                    Vector3 position = packet.ReadVector3();
                    Quaternion rotation = packet.ReadQuaternion();
                    bool setThisPlayerAsOwner = packet.ReadBool();
                    SpawnObject(user, clientPrefabKey, position, rotation, transportMethod, false, setThisPlayerAsOwner);
                    break;
                }
            case CommandType.OBJECT_DESTROY_REQUEST:
                {
                    ushort objectId = packet.ReadUShort();
                    if (ServerObjects.TryGetValue(objectId, out ServerObject serverObject))
                    {
                        DestroyObject(serverObject);
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
                    ServerObjects.TryGetValue(objectId, out ServerObject serverObject);
                    if (serverObject != null)
                    {
                        serverObject.ReceiveDataUnconnected(ipEndPoint, packet, objectServiceType, objectCommand);
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
        // Initialize starting objects (AKA objects already placed on the map) for the joining user
        lobby.SendToUser(joinedUser, PacketBuilder.ObjectsInit(spawnedStartingObjectIds.ToArray()), TransportMethod.Reliable);
        // Destroy any starting objects that have already been destroyed by other players
        foreach (ushort destroyedObjectId in destroyedStartingObjectIds)
        {
            lobby.SendToUser(joinedUser, PacketBuilder.ObjectDestroy(destroyedObjectId), TransportMethod.Reliable);
        }

        // Spawn the rest of the objects for the joining user (not starting objects and not player objects)
        // Spawning happens first in the Server Service
        foreach (ServerObject obj in ServerObjects.Values.Where(o => !spawnedStartingObjectIds.Contains(o.Id) && o.Id >= byte.MaxValue))
        {
            Tuple<int, string> clientPrefabInfo = NetResources.Instance.GetClientPrefabFromServerKey(obj.PrefabKey);
            if (clientPrefabInfo != null)
            {
                lobby.SendToUser(joinedUser, PacketBuilder.ObjectSpawn(obj.Id, clientPrefabInfo.Item1, obj.transform.position, obj.transform.rotation, obj.Owner ? (byte?)obj.Owner.Id : null), TransportMethod.Reliable);
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
            ServerObject serverObj = InstantiateOnServer(serverPrefabInfo.Item2, position, rotation, false, setThisPlayerAsOwner ? lobby.GetService<PlayerServerService>().ServerPlayers[spawningUser] : null);
            serverObj.Init(lobby.GenerateObjectId(), lobby);

            if (isStartingObject)
            {
                spawnedStartingObjectIds.Add(serverObj.Id);
            }
            else
            {
                lobby.SendToGame(PacketBuilder.ObjectSpawn(serverObj.Id, clientPrefabKey, serverObj.transform.position, serverObj.transform.rotation, serverObj.Owner ? (byte?)serverObj.Owner.Id : null), transportMethod ?? TransportMethod.Reliable);
            }
        }
    }

    public void DestroyObject(ServerObject serverObj)
    {
        if (spawnedStartingObjectIds.Contains(serverObj.Id))
        {
            destroyedStartingObjectIds.Add(serverObj.Id);
        }

        DestroyOnServer(serverObj, true);
    }
}
