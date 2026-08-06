using System;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Monstroe.CNetworkingSolution
{
    public abstract class ServerBehaviour : MonoBehaviour, INetEvent, INetRpc
    {
        public bool ForwardUnknownPacketToClient => forwardUnknownPacketToClient;
        public bool ForwardUnknownRPCToClient => forwardUnknownRPCToClient;

        [Header("ServerBehaviour Settings")]
        [SerializeField] private bool forwardUnknownPacketToClient = false;
        [SerializeField] private bool forwardUnknownRPCToClient = false;

        protected ServerLobby lobby;
        protected Type type;

        public virtual void Init(ServerLobby lobby)
        {
            this.lobby = lobby;
            this.type = GetType();
            lobby.RegisterRpcContainer(this);
            lobby.RegisterGameEventListener(this);
        }

        public virtual void Remove()
        {
            lobby.UnregisterRpcContainer(this);
            lobby.UnregisterGameEventListener(this);
        }

        internal void ReceiveData(UserData user, ulong serviceId, NetPacket packet, ushort commandType, TransportMethod? transportMethod)
        {
            if ((ReservedCommandType)commandType == ReservedCommandType.RPC)
            {
                ulong methodId = packet.ReadULong();
                if (lobby.RpcBus.TryGetRpcMethodByInstanceAndId(this, methodId, out MethodInfo method) && method.GetCustomAttribute<ClientRpcAttribute>() == null)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    object[] args = new object[parameters.Length];
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (parameters[i].ParameterType == typeof(UserData) && parameters[i].GetCustomAttribute<RpcSenderAttribute>() != null)
                        {
                            args[i] = user;
                        }
                        else
                        {
                            args[i] = packet.ReadObject(parameters[i].ParameterType);
                        }
                    }

                    method.Invoke(this, args);
                }
                else
                {
                    if (ForwardUnknownRPCToClient)
                    {
                        lobby.SendToGame(serviceId, packet, transportMethod ?? TransportMethod.Reliable);
                    }
                    else
                    {
                        Debug.LogError($"<color=red><b>CNS</b></color>: RPC Method with ID {methodId} not found on ServerBehavior {type.Name}.");
                    }
                }
            }
            else
            {
                bool packetHandled = ReceiveData(user, packet, commandType, transportMethod);
                if (!packetHandled && ForwardUnknownPacketToClient)
                {
                    lobby.SendToGame(serviceId, packet, transportMethod ?? TransportMethod.Reliable);
                }
            }
        }

        internal void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ulong serviceId, ushort commandType)
        {
            bool packetHandled = ReceiveDataUnconnected(ipEndPoint, packet, commandType);
            if (!packetHandled && ForwardUnknownPacketToClient)
            {
                lobby.SendToUnconnected(ipEndPoint, serviceId, packet);
            }
        }

        public virtual bool ReceiveData(UserData user, NetPacket packet, ushort commandType, TransportMethod? transportMethod) { return false; }
        public virtual bool ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ushort commandType) { return false; }

        public virtual void EarlyUserJoined(UserData joinedUser) { }
        public virtual void UserJoined(UserData joinedUser) { }
        public virtual void LateUserJoined(UserData joinedUser) { }

        public virtual void EarlyUserJoinedGame(UserData joinedUser) { }
        public virtual void UserJoinedGame(UserData joinedUser) { }
        public virtual void LateUserJoinedGame(UserData joinedUser) { }

        public virtual void EarlyUserLeftGame(UserData leftUser) { }
        public virtual void UserLeftGame(UserData leftUser) { }
        public virtual void LateUserLeftGame(UserData leftUser) { }

        public virtual void EarlyUserLeft(UserData leftUser) { }
        public virtual void UserLeft(UserData leftUser) { }
        public virtual void LateUserLeft(UserData leftUser) { }

        public virtual void Tick() { }

        protected ServerObject InstantiateOnServer(string originalPath, byte? ownerId = null, bool initAndSendToUsers = true)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
            ServerObject instance = InstantiateOnServer(handle, ownerId, initAndSendToUsers);
            Addressables.Release(handle);
            return instance;
        }

        protected ServerObject InstantiateOnServer(string originalPath, Vector3 position, Quaternion rotation, byte? ownerId = null, bool initAndSendToUsers = true)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
            ServerObject instance = InstantiateOnServer(handle, position, rotation, ownerId, initAndSendToUsers);
            Addressables.Release(handle);
            return instance;
        }

        protected ServerObject InstantiateOnServer(string originalPath, Vector3 position, Quaternion rotation, Transform parent, byte? ownerId = null, bool initAndSendToUsers = true)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
            ServerObject instance = InstantiateOnServer(handle, position, rotation, parent, ownerId, initAndSendToUsers);
            Addressables.Release(handle);
            return instance;
        }

        protected ServerObject InstantiateOnServer(string originalPath, Transform parent, byte? ownerId = null, bool initAndSendToUsers = true)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
            ServerObject instance = InstantiateOnServer(handle, parent, ownerId, initAndSendToUsers);
            Addressables.Release(handle);
            return instance;
        }

        protected ServerObject InstantiateOnServer(GameObject original, byte? ownerId = null, bool initAndSendToUsers = true)
        {
            Instantiate(original).TryGetComponent(out ServerObject instance);
            InitInstance(instance, lobby.transform, ownerId, false, initAndSendToUsers);
            return instance;
        }

        protected ServerObject InstantiateOnServer(GameObject original, Vector3 position, Quaternion rotation, byte? ownerId = null, bool initAndSendToUsers = true)
        {
            Instantiate(original, position, rotation).TryGetComponent(out ServerObject instance);
            InitInstance(instance, lobby.transform, ownerId, false, initAndSendToUsers);
            return instance;
        }

        protected ServerObject InstantiateOnServer(GameObject original, Vector3 position, Quaternion rotation, Transform parent, byte? ownerId = null, bool initAndSendToUsers = true)
        {
            Instantiate(original, position, rotation).TryGetComponent(out ServerObject instance);
            InitInstance(instance, parent, ownerId, false, initAndSendToUsers);
            return instance;
        }

        protected ServerObject InstantiateOnServer(GameObject original, Transform parent, byte? ownerId = null, bool initAndSendToUsers = true)
        {
            Instantiate(original).TryGetComponent(out ServerObject instance);
            InitInstance(instance, parent, ownerId, false, initAndSendToUsers);
            return instance;
        }

        protected ServerObject InstantiateOnServerAsPlayer(string originalPath, byte ownerId, bool initAndSendToUsers = true)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
            ServerObject instance = InstantiateOnServerAsPlayer(handle, ownerId, initAndSendToUsers);
            Addressables.Release(handle);
            return instance;
        }

        protected ServerObject InstantiateOnServerAsPlayer(string originalPath, Vector3 position, Quaternion rotation, byte ownerId, bool initAndSendToUsers = true)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
            ServerObject instance = InstantiateOnServerAsPlayer(handle, position, rotation, ownerId, initAndSendToUsers);
            Addressables.Release(handle);
            return instance;
        }

        protected ServerObject InstantiateOnServerAsPlayer(string originalPath, Vector3 position, Quaternion rotation, Transform parent, byte ownerId, bool initAndSendToUsers = true)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
            ServerObject instance = InstantiateOnServerAsPlayer(handle, position, rotation, parent, ownerId, initAndSendToUsers);
            Addressables.Release(handle);
            return instance;
        }

        protected ServerObject InstantiateOnServerAsPlayer(string originalPath, Transform parent, byte ownerId, bool initAndSendToUsers = true)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
            ServerObject instance = InstantiateOnServerAsPlayer(handle, parent, ownerId, initAndSendToUsers);
            Addressables.Release(handle);
            return instance;
        }

        protected ServerObject InstantiateOnServerAsPlayer(GameObject original, byte ownerId, bool initAndSendToUsers = true)
        {
            Instantiate(original).TryGetComponent(out ServerObject instance);
            InitInstance(instance, lobby.transform, ownerId, true, initAndSendToUsers);
            return instance;
        }

        protected ServerObject InstantiateOnServerAsPlayer(GameObject original, Vector3 position, Quaternion rotation, byte ownerId, bool initAndSendToUsers = true)
        {
            Instantiate(original, position, rotation).TryGetComponent(out ServerObject instance);
            InitInstance(instance, lobby.transform, ownerId, true, initAndSendToUsers);
            return instance;
        }

        protected ServerObject InstantiateOnServerAsPlayer(GameObject original, Vector3 position, Quaternion rotation, Transform parent, byte ownerId, bool initAndSendToUsers = true)
        {
            Instantiate(original, position, rotation).TryGetComponent(out ServerObject instance);
            InitInstance(instance, parent, ownerId, true, initAndSendToUsers);
            return instance;
        }

        protected ServerObject InstantiateOnServerAsPlayer(GameObject original, Transform parent, byte ownerId, bool initAndSendToUsers = true)
        {
            Instantiate(original).TryGetComponent(out ServerObject instance);
            InitInstance(instance, parent, ownerId, true, initAndSendToUsers);
            return instance;
        }

        private void InitInstance(ServerObject instance, Transform parent, byte? ownerId, bool isPlayer, bool initAndSendToUsers)
        {
            if (instance != null)
            {
                if (lobby.LobbyScene.HasValue)
                {
                    SceneManager.MoveGameObjectToScene(instance.gameObject, lobby.LobbyScene.Value);
                }
                instance.transform.SetParent(parent);

                instance.OwnerId = ownerId;
                instance.Owner = ownerId != null ? lobby.LobbyData.GameUsers.FirstOrDefault(u => u.PlayerId == ownerId) : null;
                instance.IsPlayer = isPlayer;

                if (initAndSendToUsers)
                {
                    ushort id = lobby.GenerateObjectId();
                    Tuple<ulong, string> clientPrefabInfo = NetResources.Instance.GetClientPrefabFromServerKey(instance.PrefabKey);
                    if (clientPrefabInfo != null)
                    {
                        lobby.SendToGame<ObjectServerService>(ObjectPacketBuilder.ObjectSpawn(id, clientPrefabInfo.Item1, instance.transform.position, instance.transform.rotation, isPlayer, ownerId), TransportMethod.Reliable);
                    }
                    instance.Init(id, lobby);
                }
            }
        }

        protected void DestroyOnServer(ServerObject serverObj, bool sendToUsers = true)
        {
            serverObj.Remove();
            Destroy(serverObj.gameObject);
            if (sendToUsers)
            {
                lobby.SendToGame<ObjectServerService>(ObjectPacketBuilder.ObjectDestroy(serverObj.Id), TransportMethod.Reliable);
            }
        }
    }
}
