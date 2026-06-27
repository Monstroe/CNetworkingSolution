using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

namespace Monstroe.CNetworkingSolution
{
    [ServiceId("ObjectService")]
    public class ObjectServerService : ServerService
    {
        public Dictionary<ushort, ServerObject> ServerObjects { get; private set; } = new Dictionary<ushort, ServerObject>();
        public Dictionary<ushort, ServerTransform> ServerTransforms { get; private set; } = new Dictionary<ushort, ServerTransform>();

        public NetMap Map { get; private set; }

        [Header("Map Settings")]
        [Tooltip("The map prefab to be instantiated on the server.")]
        [SerializeField] private NetMap mapPrefab;
        [SerializeField] private bool hideMapMesh = true;

        private bool startingObjectsInitialized = false;
        private List<ushort> spawnedStartingObjectIds = new List<ushort>();
        private List<ushort> destroyedStartingObjectIds = new List<ushort>();

        public override void Init(ServerLobby lobby)
        {
            base.Init(lobby);
            // Init Map
            Map = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity).GetComponent<NetMap>();
            Map.transform.SetParent(this.transform);

            if (hideMapMesh)
            {
                foreach (Renderer r in Map.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = false;
                }
            }

            foreach (ClientObject obj in Map.GetComponentsInChildren<ClientObject>(true))
            {
                if (obj.gameObject.TryGetComponent(out Collider objCollider))
                {
                    objCollider.enabled = false;
                }

                foreach (Renderer r in obj.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = false;
                }

                obj.enabled = false;
            }
        }

        public override async void ReceiveData(UserData user, NetPacket packet, ushort commandType, TransportMethod? transportMethod)
        {
            base.ReceiveData(user, packet, commandType, transportMethod);
            switch ((ObjectCommandType)commandType)
            {
                case ObjectCommandType.OBJECT_COMMUNICATION:
                    {
                        ushort objectId = packet.ReadUShort();
                        ushort objectCommand = packet.ReadUShort();
                        if (ServerObjects.TryGetValue(objectId, out ServerObject serverObject))
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

                        ObjectSpawnRequestReceivedEvent spawnRequestEvent = new ObjectSpawnRequestReceivedEvent()
                        {
                            ClientPrefabKey = clientPrefabKey,
                            Position = position,
                            Rotation = rotation
                        };
                        var result = await lobby.TriggerGameEvent(spawnRequestEvent);
                        if (!result.Canceled)
                        {
                            SpawnObject(user, clientPrefabKey, position, rotation, transportMethod, false, false);
                        }
                        break;
                    }
                case ObjectCommandType.OBJECT_DESTROY_REQUEST:
                    {
                        ushort objectId = packet.ReadUShort();
                        if (ServerObjects.TryGetValue(objectId, out ServerObject serverObject) && user.PlayerId == serverObject.OwnerId)
                        {
                            ObjectDestroyRequestReceivedEvent destroyRequestEvent = new ObjectDestroyRequestReceivedEvent()
                            {
                                ObjectId = objectId
                            };
                            var result = await lobby.TriggerGameEvent(destroyRequestEvent);
                            if (!result.Canceled)
                            {
                                DestroyObject(serverObject);
                            }
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

        public override void EarlyUserJoined(UserData joinedUser)
        {
            base.EarlyUserJoined(joinedUser);
            foreach (var serverObject in ServerObjects.Values)
            {
                serverObject.EarlyUserJoined(joinedUser);
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

        public override void LateUserJoined(UserData joinedUser)
        {
            base.LateUserJoined(joinedUser);
            foreach (var serverObject in ServerObjects.Values)
            {
                serverObject.LateUserJoined(joinedUser);
            }
        }

        public override void EarlyUserJoinedGame(UserData joinedUser)
        {
            base.EarlyUserJoinedGame(joinedUser);
            if (!startingObjectsInitialized)
            {
                foreach (ClientObject clientObj in Map.GetStartingClientObjects())
                {
                    SpawnObject(joinedUser, clientObj.PrefabKey, clientObj.transform.position, clientObj.transform.rotation, null, true, false);
                }
                startingObjectsInitialized = true;
            }

            // Initialize starting objects (AKA objects already placed on the map) for the joining user
            SendToUserClientService(joinedUser, ObjectPacketBuilder.ObjectsInit(spawnedStartingObjectIds.ToArray()), TransportMethod.Reliable);
            // Destroy any starting objects that have already been destroyed by other players
            foreach (ushort destroyedObjectId in destroyedStartingObjectIds)
            {
                SendToUserClientService(joinedUser, ObjectPacketBuilder.ObjectDestroy(destroyedObjectId), TransportMethod.Reliable);
            }

            // Spawn the rest of the objects for the joining user (not starting objects and not player objects)
            // Spawning happens first in the Server Service
            foreach (ServerObject obj in ServerObjects.Values.Where(o => !spawnedStartingObjectIds.Contains(o.Id)))
            {
                Tuple<ulong, string> clientPrefabInfo = NetResources.Instance.GetClientPrefabFromServerKey(obj.PrefabKey);
                if (clientPrefabInfo != null)
                {
                    SendToUserClientService(joinedUser, ObjectPacketBuilder.ObjectSpawn(obj.Id, clientPrefabInfo.Item1, obj.transform.position, obj.transform.rotation, obj.Id <= byte.MaxValue, obj.OwnerId), TransportMethod.Reliable);
                }
            }

            // Handle EarlyUserJoinedGame for all existing objects
            foreach (var serverObject in ServerObjects.Values)
            {
                serverObject.EarlyUserJoinedGame(joinedUser);
            }
        }

        public override void UserJoinedGame(UserData joinedUser)
        {
            base.UserJoinedGame(joinedUser);
            foreach (var serverObject in ServerObjects.Values)
            {
                serverObject.UserJoinedGame(joinedUser);
            }
        }

        public override void LateUserJoinedGame(UserData joinedUser)
        {
            base.LateUserJoinedGame(joinedUser);
            foreach (var serverObject in ServerObjects.Values)
            {
                serverObject.LateUserJoinedGame(joinedUser);
            }
        }

        public override void UserLeftGame(UserData leftUser)
        {
            base.UserLeftGame(leftUser);
            foreach (var serverObject in ServerObjects.Values)
            {
                serverObject.UserLeftGame(leftUser);
            }
        }

        public override void LateUserLeftGame(UserData leftUser)
        {
            base.LateUserLeftGame(leftUser);
            foreach (var serverObject in ServerObjects.Values)
            {
                serverObject.LateUserLeftGame(leftUser);
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

        public override void LateUserLeft(UserData leftUser)
        {
            base.LateUserLeft(leftUser);
            foreach (var serverObject in ServerObjects.Values)
            {
                serverObject.LateUserLeft(leftUser);
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

        private void SpawnObject(UserData spawningUser, ulong clientPrefabKey, Vector3 position, Quaternion rotation, TransportMethod? transportMethod, bool isStartingObject, bool setThisPlayerAsOwner)
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
                    SendToGameClientServices(ObjectPacketBuilder.ObjectSpawn(id, clientPrefabKey, serverObj.transform.position, serverObj.transform.rotation, false, serverObj.OwnerId), transportMethod ?? TransportMethod.Reliable);
                }

                serverObj.Init(id, lobby);
            }
        }

        private void DestroyObject(ServerObject serverObj)
        {
            if (spawnedStartingObjectIds.Contains(serverObj.Id))
            {
                destroyedStartingObjectIds.Add(serverObj.Id);
            }

            DestroyOnServer(serverObj, true);
        }
    }

    public class ObjectSpawnRequestReceivedEvent : GameEvent
    {
        public ulong ClientPrefabKey { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
    }

    public class ObjectDestroyRequestReceivedEvent : GameEvent
    {
        public ushort ObjectId { get; set; }
    }
}