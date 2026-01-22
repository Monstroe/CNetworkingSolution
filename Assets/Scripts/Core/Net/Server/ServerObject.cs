using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public abstract class ServerObject : ServerBehaviour, INetObject
{
    public ushort Id { get; private set; }

    public ServerPlayer Owner { get; set; } = null;

    public int PrefabKey => prefabKey;
    public string PrefabPath => prefabPath;

    [SerializeField, HideInInspector]
    private int prefabKey;
    [SerializeField, HideInInspector]
    private string prefabPath;


    private sealed class RpcMethod
    {
        public ushort MethodId;
        public MethodInfo Method;
        public RpcAttribute Attribute;
    }

    private Type type;
    private static Dictionary<Type, Dictionary<string, RpcMethod>> rpcMethods = new Dictionary<Type, Dictionary<string, RpcMethod>>();

    public virtual void Init(ushort id, ServerLobby lobby)
    {
        this.Id = id;
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
                        MethodId = lobby.GetService<RpcServerService>().Bus.GenerateRpcMethodId(method)
                    };
                }
            }
        }

        //lobby.GetService<EventServerSerivce>().Bus.RegisterListener(this);
        lobby.GetService<RpcServerService>().Bus.RegisterRpcContainer(this);
        lobby.GetService<ObjectServerService>().ServerObjects.Add(id, this);
    }

    public virtual void Remove()
    {
        //lobby.GetService<EventServerSerivce>().Bus.UnregisterListener(this);
        lobby.GetService<RpcServerService>().Bus.UnregisterRpcContainer(this);
        lobby.GetService<ObjectServerService>().ServerObjects.Remove(Id);
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

    public virtual void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.RPC_INVOKE:
                {
                    ushort methodId = packet.ReadUShort();
                    MethodInfo method = lobby.GetService<RpcServerService>().Bus.GetRpcMethod(this, methodId);
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
    public abstract void Tick();
    public abstract void UserJoined(UserData joinedUser);
    public abstract void UserJoinedGame(UserData joinedUser);
    public abstract void UserLeft(UserData leftUser);

    public void SendToGameClientObjects(NetPacket packet, TransportMethod transportMethod, UserData exception = null)
    {
        lobby.SendToGame(PacketBuilder.ObjectCommunication(this, packet), transportMethod, exception);
    }

    public void SendToUserClientObject(UserData user, NetPacket packet, TransportMethod transportMethod)
    {
        lobby.SendToUser(user, PacketBuilder.ObjectCommunication(this, packet), transportMethod);
    }

    public void InvokeOnGameClientObjects(string methodName, params object[] args)
    {
        if (rpcMethods[type].TryGetValue(methodName, out RpcMethod method))
        {
            SendToGameClientObjects(PacketBuilder.RpcInvoke(method.MethodId, args), method.Attribute.TransportMethod);
        }
        else
        {
            Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
        }
    }
}