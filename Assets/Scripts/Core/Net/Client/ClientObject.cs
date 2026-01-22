using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public abstract class ClientObject : ClientBehaviour, INetObject
{
    public ushort Id { get; private set; }

    public ClientPlayer Owner { get; set; } = null;

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

    private sealed class RpcMethod
    {
        public ushort MethodId;
        public MethodInfo Method;
        public RpcAttribute Attribute;
    }
    private Type type;
    private static Dictionary<Type, Dictionary<string, RpcMethod>> rpcMethods = new Dictionary<Type, Dictionary<string, RpcMethod>>();

    public virtual void Init(ushort id, ClientLobby lobby)
    {
        Id = id;
        this.lobby = lobby;

        type = GetType();
        if (!rpcMethods.ContainsKey(type))
        {
            rpcMethods[type] = new Dictionary<string, RpcMethod>();
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<RpcAttribute>() is RpcAttribute attr)
                {
                    if (rpcMethods[type].ContainsKey(method.Name))
                    {
                        throw new Exception($"Overloaded RPC methods are not allowed: {type.Name}.{method.Name}");
                    }

                    rpcMethods[type][method.Name] = new RpcMethod()
                    {
                        Method = method,
                        Attribute = attr,
                        MethodId = lobby.GetService<RpcClientService>().Bus.GenerateRpcMethodId(method)
                    };
                }
            }
        }

        lobby.GetService<RpcClientService>().Bus.RegisterRpcContainer(this);
        lobby.GetService<ObjectClientService>().ClientObjects.Add(id, this);
    }

    public virtual void Remove()
    {
        lobby.GetService<RpcClientService>().Bus.UnregisterRpcContainer(this);
        lobby.GetService<ObjectClientService>().ClientObjects.Remove(Id);
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

    protected virtual void AwakeOnOwner() { }
    protected virtual void AwakeOnNonOwner() { }
    protected virtual void Awake()
    {
        if (Owner == Player.Instance)
        {
            AwakeOnOwner();
        }
        else
        {
            AwakeOnNonOwner();
        }
    }

    protected virtual void StartOnOwner() { }
    protected virtual void StartOnNonOwner() { }
    protected virtual void Start()
    {
        if (Owner == Player.Instance)
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
        if (Owner == Player.Instance)
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
        if (Owner == Player.Instance)
        {
            FixedUpdateOnOwner();
        }
        else
        {
            FixedUpdateOnNonOwner();
        }
    }

    public virtual void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.RPC_INVOKE:
                {
                    ushort methodId = packet.ReadUShort();
                    MethodInfo method = lobby.GetService<RpcClientService>().Bus.GetRpcMethod(this, methodId);
                    ParameterInfo[] parameters = method.GetParameters();
                    object[] args = new object[parameters.Length];
                    for (int i = 0; i < args.Length; i++)
                    {
                        args[i] = packet.ReadObject(parameters[i].ParameterType);
                    }

                    method.Invoke(this, args);
                    break;
                }
        }
    }
    public virtual void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType) { }

    public void SendToServerObject(NetPacket packet, TransportMethod transportMethod)
    {
        lobby.SendToServer(PacketBuilder.ObjectCommunication(this, packet), transportMethod);
    }

    public void InvokeOnServerObject(string methodName, params object[] args)
    {
        if (rpcMethods[type].TryGetValue(methodName, out RpcMethod method))
        {
            SendToServerObject(PacketBuilder.RpcInvoke(method.MethodId, args), method.Attribute.TransportMethod);
        }
        else
        {
            Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
        }
    }
}
