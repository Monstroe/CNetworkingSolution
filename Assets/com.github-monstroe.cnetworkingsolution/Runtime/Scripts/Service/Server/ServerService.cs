using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public abstract class ServerService : ServerBehaviour
{
    public int ExecutionOrder => executionOrder;
    public ulong ServiceId => serviceId;

    [SerializeField, HideInInspector]
    private ulong serviceId;

    [Header("Server Service Settings")]
    [SerializeField] protected int executionOrder = 0;

    public override void Init(ServerLobby lobby)
    {
        base.Init(lobby);
        if (lobby.RegisterService(this, out serviceId))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: ServerService {type.Name} registered.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {type.Name} is already registered.");
        }
    }

    public override void Remove()
    {
        if (lobby.UnregisterService(this))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: ServerService {type.Name} unregistered.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {type.Name} was not registered.");
        }
        base.Remove();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
            return;
        ResetServiceId(GetType());
    }

    internal void ResetServiceId(Type type)
    {
        EditorUtility.SetDirty(this);
        serviceId = ServiceBus.GetServiceId(type);
    }
#endif

    public void SendToLobbyClientServices(NetPacket packet, TransportMethod transportMethod, UserData exception = null)
    {
        lobby.SendToLobby(serviceId, packet, transportMethod, exception);
    }

    public void InvokeOnLobbyClientServices(string methodName, params object[] args)
    {
        InvokeOnLobbyClientServices(methodName, null, args);
    }

    public void InvokeOnLobbyClientServices(string methodName, UserData exception = null, params object[] args)
    {
        if (lobby.RpcBus.TryGetRpcMethodByTypeAndName(type, methodName, out ulong methodId, out MethodInfo method, out RpcAttribute attr))
        {
            SendToLobbyClientServices(ReservedPacketBuilder.Rpc(methodId, method, args), attr.TransportMethod, exception);
        }
        else
        {
            Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
        }
    }

    public void SendToGameClientServices(NetPacket packet, TransportMethod transportMethod, UserData exception = null)
    {
        lobby.SendToGame(serviceId, packet, transportMethod, exception);
    }

    public void InvokeOnGameClientServices(string methodName, params object[] args)
    {
        InvokeOnGameClientServices(methodName, null, args);
    }

    public void InvokeOnGameClientServices(string methodName, UserData exception = null, params object[] args)
    {
        if (lobby.RpcBus.TryGetRpcMethodByTypeAndName(type, methodName, out ulong methodId, out MethodInfo method, out RpcAttribute attr))
        {
            SendToGameClientServices(ReservedPacketBuilder.Rpc(methodId, method, args), attr.TransportMethod, exception);
        }
        else
        {
            Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
        }
    }

    public void SendToUserClientService(UserData user, NetPacket packet, TransportMethod transportMethod)
    {
        lobby.SendToUser(user, serviceId, packet, transportMethod);
    }

    public void InvokeOnUserClientService(UserData user, string methodName, params object[] args)
    {
        if (lobby.RpcBus.TryGetRpcMethodByTypeAndName(type, methodName, out ulong methodId, out MethodInfo method, out RpcAttribute attr))
        {
            SendToUserClientService(user, ReservedPacketBuilder.Rpc(methodId, method, args), attr.TransportMethod);
        }
        else
        {
            Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
        }
    }
}