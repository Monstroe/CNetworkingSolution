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

        internal bool ReceiveData(UserData user, NetPacket packet, ulong serviceId, ushort commandType, TransportMethod? transportMethod)
        {
            bool packetHandled = true;
            if ((ReservedCommandType)commandType == ReservedCommandType.RPC)
            {
                ulong memberId = packet.ReadULong();
                if (lobby.RpcBus.TryGetRpcMemberByInstanceAndId(this, memberId, out MemberInfo member) && member.GetCustomAttribute<ServerRpcAttribute>() != null)
                {
                    if (member is MethodInfo method)
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
                        Type memberType;
                        object currentValue;

                        switch (member)
                        {
                            case FieldInfo field:
                                memberType = field.FieldType;
                                currentValue = field.GetValue(this);
                                break;

                            case PropertyInfo property:
                                memberType = property.PropertyType;
                                currentValue = property.GetValue(this);
                                break;

                            default:
                                throw new ArgumentException($"RPC Member with ID {memberId} is not a valid RPC member on ServerBehaviour {type.Name}.");
                        }

                        if (currentValue is INetSerializable serializableValue)
                        {
                            // Use the DeserializeInto method to populate the existing instance of the member, so that we don't create a new instance and lose the reference
                            serializableValue.DeserializeInto(packet);
                        }
                        else
                        {
                            object value = packet.ReadObject(memberType);
                            switch (member)
                            {
                                case FieldInfo field:
                                    field.SetValue(this, value);
                                    break;

                                case PropertyInfo property:
                                    property.SetValue(this, value);
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    if (ForwardUnknownRPCToClient)
                    {
                        lobby.SendToGame(serviceId, packet, transportMethod ?? TransportMethod.Reliable);
                    }
                    else
                    {
                        Debug.LogError($"<color=red><b>CNS</b></color>: RPC Member with ID {memberId} not found on ServerBehavior {type.Name}.");
                    }
                }
            }
            else
            {
                packetHandled = ReceiveData(user, packet, commandType, transportMethod);
                if (!packetHandled && ForwardUnknownPacketToClient)
                {
                    lobby.SendToGame(serviceId, packet, transportMethod ?? TransportMethod.Reliable);
                }
            }

            return packetHandled;
        }

        internal bool ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ulong serviceId, ushort commandType)
        {
            bool packetHandled = ReceiveDataUnconnected(ipEndPoint, packet, commandType);
            if (!packetHandled && ForwardUnknownPacketToClient)
            {
                lobby.SendToUnconnected(ipEndPoint, serviceId, packet);
            }

            return packetHandled;
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

        protected (bool, ulong, MemberInfo, RpcAttribute) IsValidRpc(string memberName, RpcMemberType expectedType)
        {
            bool validMember = lobby.RpcBus.TryGetRpcMemberByTypeAndName(type, memberName, out ulong memberId, out MemberInfo member, out RpcMemberType memberType, out RpcAttribute attr);
            if (!validMember)
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: RPC Attribute not found on Member {type.Name}.{memberName}");
                return (false, 0, null, null);
            }

            bool isMethod = memberType == expectedType;
            if (!isMethod)
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: RPC Member {type.Name}.{memberName} is not a valid {expectedType}");
                return (false, 0, null, null);
            }

            return (true, memberId, member, attr);
        }

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
