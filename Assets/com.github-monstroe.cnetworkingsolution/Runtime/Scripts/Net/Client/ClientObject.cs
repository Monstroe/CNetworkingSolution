using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public abstract class ClientObject : ClientBehaviour, INetObject
{
    public ushort Id { get; private set; }

    public byte? OwnerId { get; private set; } = null;
    public UserData Owner { get; private set; } = null;
    public bool IsOwner { get => OwnerId == lobby.CurrentUser.PlayerId; }
    public bool IsPlayer { get; private set; } = false;
    public bool IsLocalPlayer { get => IsOwner && IsPlayer; }

    public int PrefabKey => prefabKey;
    public string PrefabPath => prefabPath;

    [SerializeField, HideInInspector]
    private int prefabKey;
    [SerializeField, HideInInspector]
    private string prefabPath;

    public ServerObject ServerPrefab => serverPrefab;

    [Header("Server Prefab")]
    [Tooltip("Reference to the corresponding server prefab for this client object.")]
    [SerializeField] private ServerObject serverPrefab;

    private Type type;
    private bool ownerInitialized = false;
    private bool isPlayerInitialized = false;

    public virtual void Init(ushort id, ClientLobby lobby)
    {
        Id = id;
        this.lobby = lobby;
        type = GetType();

        lobby.GetService<ObjectClientService>().RpcBus.RegisterRpcContainer(this);
        lobby.GetService<ObjectClientService>().ClientObjects.Add(id, this);
    }

    public virtual void Remove()
    {
        lobby.GetService<ObjectClientService>().RpcBus.UnregisterRpcContainer(this);
        lobby.GetService<ObjectClientService>().ClientObjects.Remove(Id);
    }

    public void SetOwner(byte? ownerId)
    {
        if (ownerInitialized)
        {
            Debug.LogError("SetOwner cannot be called more than once on ClientObject " + type.Name + ". Please change the owner through the server using ServerObject.SetOwner.");
            return;
        }
        SetOwnerRpc(ownerId);
    }

    public void SetAsPlayer(bool isPlayer)
    {
        if (isPlayerInitialized)
        {
            Debug.LogError("SetAsPlayer cannot be called more than once on ClientObject " + type.Name + ". Please change whether this object is a player through the server using ServerObject.SetAsPlayer.");
            return;
        }
        SetAsPlayerRpc(isPlayer);
    }

    [Rpc]
    private void SetOwnerRpc(byte? ownerId)
    {
        OwnerId = ownerId;
        Owner = OwnerId != null ? lobby.LobbyData.GameUsers.FirstOrDefault(u => u.PlayerId == OwnerId) : null;
        ownerInitialized = true;
    }

    [Rpc]
    private void SetAsPlayerRpc(bool isPlayer)
    {
        IsPlayer = isPlayer;
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

    public virtual void ReceiveData(NetPacket packet, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_RPC:
                {
                    uint methodId = packet.ReadUInt();
                    if (lobby.GetService<ObjectClientService>().RpcBus.TryGetRpcMethodByInstanceAndId(this, methodId, out MethodInfo method))
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
                        Debug.LogError($"RPC Method with ID {methodId} not found on ClientObject {type.Name}.");
                    }
                    break;
                }
        }
    }
    public virtual void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, CommandType commandType) { }

    public void SendToServerObject(NetPacket packet, TransportMethod transportMethod)
    {
        lobby.SendToServer(PacketBuilder.ObjectCommunication(this, packet), transportMethod);
    }

    public void InvokeOnServerObject(string methodName, params object[] args)
    {
        if (lobby.GetService<ObjectClientService>().RpcBus.TryGetRpcMethodByTypeAndName(type, methodName, out uint methodId, out MethodInfo method, out RpcAttribute attr))
        {
            SendToServerObject(PacketBuilder.ObjectRpc(methodId, method, args), attr.TransportMethod);
        }
        else
        {
            Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
        }
    }
}
