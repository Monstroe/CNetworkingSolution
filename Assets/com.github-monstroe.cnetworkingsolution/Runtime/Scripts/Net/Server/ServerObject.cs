using System;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public abstract class ServerObject : ServerBehaviour, INetObject
{
    public ushort Id { get; private set; }
    public byte? OwnerId { get; private set; } = null;
    public UserData Owner { get; private set; } = null;
    public bool IsPlayer { get; private set; } = false;

    public int PrefabKey => prefabKey;
    public string PrefabPath => prefabPath;

    [SerializeField, HideInInspector]
    private int prefabKey;
    [SerializeField, HideInInspector]
    private string prefabPath;

    private Type type;
    private bool ownerInitialized = false;
    private bool isPlayerInitialized = false;

    public virtual void Init(ushort id, ServerLobby lobby)
    {
        this.Id = id;
        base.Init(lobby);
        type = GetType();

        lobby.GetService<ObjectServerService>().RpcBus.RegisterRpcContainer(this);
        lobby.GetService<ObjectServerService>().EventBus.RegisterListener(this);
        lobby.GetService<ObjectServerService>().ServerObjects.Add(id, this);
    }

    public virtual void Remove()
    {
        lobby.GetService<ObjectServerService>().RpcBus.UnregisterRpcContainer(this);
        lobby.GetService<ObjectServerService>().EventBus.UnregisterListener(this);
        lobby.GetService<ObjectServerService>().ServerObjects.Remove(Id);
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

    public void ResetPrefabKeyAndPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
        }

        prefabPath = path;
        EditorUtility.SetDirty(this);
        prefabKey = NetResources.HashPathToId(prefabPath);
    }
#endif

    public virtual void ReceiveData(UserData user, NetPacket packet, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_RPC:
                {
                    uint methodId = packet.ReadUInt();
                    if (lobby.GetService<ObjectServerService>().RpcBus.TryGetRpcMethodByInstanceAndId(this, methodId, out MethodInfo method))
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        object[] args = new object[parameters.Length];
                        for (int i = 0; i < args.Length; i++)
                        {
                            args[i] = packet.ReadObject(parameters[i].ParameterType);
                        }

                        method.Invoke(this, args);
                    }
                    else
                    {
                        Debug.LogError($"RPC Method with ID {methodId} not found on ServerObject {type.Name}.");
                    }
                    break;
                }
        }
    }
    public virtual void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, CommandType commandType) { }
    public abstract void Tick();
    public abstract void UserJoined(UserData joinedUser);
    public abstract void UserJoinedGame(UserData joinedUser);
    public abstract void UserLeft(UserData leftUser);

    public void SendToGameClientObjects(NetPacket packet, TransportMethod transportMethod, UserData exception = null)
    {
        lobby.SendToGame(PacketBuilder.ObjectCommunication(this, packet), transportMethod, exception);
    }

    public void InvokeOnGameClientObjects(string methodName, params object[] args)
    {
        if (lobby.GetService<ObjectServerService>().RpcBus.TryGetRpcMethodByTypeAndName(type, methodName, out uint methodId, out MethodInfo method, out RpcAttribute attr))
        {
            SendToGameClientObjects(PacketBuilder.ObjectRpc(methodId, method, args), attr.TransportMethod);
        }
        else
        {
            Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
        }
    }

    public void SendToUserClientObject(UserData user, NetPacket packet, TransportMethod transportMethod)
    {
        lobby.SendToUser(user, PacketBuilder.ObjectCommunication(this, packet), transportMethod);
    }

    public void InvokeOnUserClientObject(UserData user, string methodName, params object[] args)
    {
        if (lobby.GetService<ObjectServerService>().RpcBus.TryGetRpcMethodByTypeAndName(type, methodName, out uint methodId, out MethodInfo method, out RpcAttribute attr))
        {
            SendToUserClientObject(user, PacketBuilder.ObjectRpc(methodId, method, args), attr.TransportMethod);
        }
        else
        {
            Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
        }
    }
}