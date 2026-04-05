using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public abstract class ServerObject : ServerBehaviour, INetObject
{
    public ushort Id { get; private set; }
    public byte? OwnerId { get; private set; } = null;
    public UserData Owner { get; private set; } = null;
    public bool IsPlayer { get; private set; } = false;

    public ulong PrefabKey => prefabKey;
    public string PrefabPath => prefabPath;

    [SerializeField, HideInInspector]
    private ulong prefabKey;
    [SerializeField, HideInInspector]
    private string prefabPath;

    private bool ownerInitialized = false;
    private bool isPlayerInitialized = false;

    public virtual void Init(ushort id, ServerLobby lobby)
    {
        Id = id;
        base.Init(lobby);

        lobby.GetService<ObjectServerService>().ServerObjects.Add(id, this);
    }

    public override void Remove()
    {
        lobby.GetService<ObjectServerService>().ServerObjects.Remove(Id);
        base.Remove();
    }

    public void SetOwner(byte? ownerId)
    {
        SetOwnerRpc(ownerId);
    }

    public void SetAsPlayer(bool isPlayer)
    {
        SetAsPlayerRpc(isPlayer);
    }

    [Rpc]
    private void SetOwnerRpc(byte? ownerId)
    {
        OwnerId = ownerId;
        Owner = OwnerId != null ? lobby.LobbyData.GameUsers.FirstOrDefault(u => u.PlayerId == OwnerId) : null;
        if (ownerInitialized)
        {
            InvokeOnGameClientObjects(nameof(SetOwnerRpc), ownerId);
        }
        ownerInitialized = true;
    }

    [Rpc]
    private void SetAsPlayerRpc(bool isPlayer)
    {
        IsPlayer = isPlayer;
        if (isPlayerInitialized)
        {
            InvokeOnGameClientObjects(nameof(SetAsPlayerRpc), isPlayer);
        }
        isPlayerInitialized = true;
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

    public void SendToLobbyClientObjects(NetPacket packet, TransportMethod transportMethod, UserData exception = null)
    {
        lobby.SendToLobby<ObjectServerService>(ObjectPacketBuilder.ObjectCommunication(this, packet), transportMethod, exception);
    }

    public void InvokeOnLobbyClientObjects(string methodName, params object[] args)
    {
        if (lobby.RpcBus.TryGetRpcMethodByTypeAndName(type, methodName, out ulong methodId, out MethodInfo method, out RpcAttribute attr))
        {
            SendToLobbyClientObjects(ReservedPacketBuilder.Rpc(methodId, method, args), attr.TransportMethod);
        }
        else
        {
            Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
        }
    }

    public void SendToGameClientObjects(NetPacket packet, TransportMethod transportMethod, UserData exception = null)
    {
        lobby.SendToGame<ObjectServerService>(ObjectPacketBuilder.ObjectCommunication(this, packet), transportMethod, exception);
    }

    public void InvokeOnGameClientObjects(string methodName, params object[] args)
    {
        if (lobby.RpcBus.TryGetRpcMethodByTypeAndName(type, methodName, out ulong methodId, out MethodInfo method, out RpcAttribute attr))
        {
            SendToGameClientObjects(ReservedPacketBuilder.Rpc(methodId, method, args), attr.TransportMethod);
        }
        else
        {
            Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
        }
    }

    public void SendToUserClientObject(UserData user, NetPacket packet, TransportMethod transportMethod)
    {
        lobby.SendToUser<ObjectServerService>(user, ObjectPacketBuilder.ObjectCommunication(this, packet), transportMethod);
    }

    public void InvokeOnUserClientObject(UserData user, string methodName, params object[] args)
    {
        if (lobby.RpcBus.TryGetRpcMethodByTypeAndName(type, methodName, out ulong methodId, out MethodInfo method, out RpcAttribute attr))
        {
            SendToUserClientObject(user, ReservedPacketBuilder.Rpc(methodId, method, args), attr.TransportMethod);
        }
        else
        {
            Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
        }
    }
}