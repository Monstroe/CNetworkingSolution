using System.Linq;
using System.Net;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Monstroe.CNetworkingSolution
{
    public abstract class ClientObject : ClientBehaviour, INetObject
    {
        public ushort Id { get; private set; }

        public byte? OwnerId { get; internal set; } = null;
        public UserData Owner { get; internal set; } = null;
        public bool IsOwner { get => OwnerId == lobby.CurrentUser.PlayerId; }
        public bool IsPlayer { get; internal set; } = false;
        public bool IsLocalPlayer { get => IsOwner && IsPlayer; }

        public ulong PrefabKey => prefabKey;
        public string PrefabPath => prefabPath;

        [SerializeField, HideInInspector]
        private ulong prefabKey;
        [SerializeField, HideInInspector]
        private string prefabPath;

        public ServerObject ServerPrefab => serverPrefab;

        [Header("Server Prefab")]
        [Tooltip("Reference to the corresponding server prefab for this client object.")]
        [SerializeField] private ServerObject serverPrefab;

        public virtual void Init(ushort id, ClientLobby lobby)
        {
            Id = id;
            base.Init(lobby);

            lobby.GetService<ObjectClientService>().ClientObjects.Add(id, this);
        }

        public override void Remove()
        {
            lobby.GetService<ObjectClientService>().ClientObjects.Remove(Id);
            base.Remove();
        }

        [ClientRpc]
        private void SetOwnerRpc(byte? ownerId)
        {
            OwnerId = ownerId;
            Owner = OwnerId != null ? lobby.LobbyData.GameUsers.FirstOrDefault(u => u.PlayerId == OwnerId) : null;
        }

        [ClientRpc]
        private void SetAsPlayerRpc(bool isPlayer)
        {
            IsPlayer = isPlayer;
        }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
        if (!string.IsNullOrEmpty(path) && prefabPath != path)
        {
            ResetPrefabKeyAndPath(path);
        }
    }

    internal void ResetPrefabKeyAndPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
        }

        prefabPath = path;
        EditorUtility.SetDirty(this);
        prefabKey = NetResources.GenerateHashKey(prefabPath);
    }
#endif

        protected virtual void StartOnOwner() { }
        protected virtual void StartOnNonOwner() { }
        protected virtual void Start()
        {
            if (IsOwner)
            {
                StartOnOwner();
            }
            else
            {
                StartOnNonOwner();
            }
        }

        protected virtual void UpdateOnOwner() { }
        protected virtual void UpdateOnNonOwner() { }
        protected virtual void Update()
        {
            if (IsOwner)
            {
                UpdateOnOwner();
            }
            else
            {
                UpdateOnNonOwner();
            }
        }

        protected virtual void FixedUpdateOnOwner() { }
        protected virtual void FixedUpdateOnNonOwner() { }
        protected virtual void FixedUpdate()
        {
            if (IsOwner)
            {
                FixedUpdateOnOwner();
            }
            else
            {
                FixedUpdateOnNonOwner();
            }
        }

        public void SendToServerObject(NetPacket packet, TransportMethod transportMethod)
        {
            lobby.SendToServer<ObjectClientService>(ObjectPacketBuilder.ObjectCommunication(this, packet), transportMethod);
        }

        public void InvokeOnServerObject(string methodName, params object[] args)
        {
            if (lobby.RpcBus.TryGetRpcMethodByTypeAndName(type, methodName, out ulong methodId, out MethodInfo method, out RpcAttribute attr))
            {
                Debug.Log($"<color=green><b>CNS</b></color>: Invoking RPC method {type.Name}.{methodName} on server with ID {methodId}.");
                SendToServerObject(ReservedPacketBuilder.Rpc(methodId, method, args), attr.TransportMethod);
            }
            else
            {
                Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
            }
        }

        public void SendToUnconnectedServerObject(IPEndPoint endPoint, NetPacket packet)
        {
            lobby.SendToUnconnected<ObjectClientService>(endPoint, ObjectPacketBuilder.ObjectCommunication(this, packet));
        }
    }
}
