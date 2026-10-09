using System;
using System.Net;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Monstroe.CNetworkingSolution
{
    public abstract class ServerService : ServerBehaviour
    {
        public int ExecutionOrder => executionOrder;
        public ulong ServiceId => serviceId;

        [SerializeField, HideInInspector]
        private ulong serviceId;

        [Header("ServerService Settings")]
        [SerializeField] protected int executionOrder = 0;
        [SerializeField] protected bool registerUnconnectedService = false;

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

            if (registerUnconnectedService)
            {
                if (lobby.RegisterUnconnectedService(this, out _))
                {
                    Debug.Log($"<color=green><b>CNS</b></color>: Unconnected ServerService {type.Name} registered.");
                }
                else
                {
                    Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ServerService {type.Name} is already registered.");
                }
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

            if (registerUnconnectedService)
            {
                if (lobby.UnregisterUnconnectedService(this))
                {
                    Debug.Log($"<color=green><b>CNS</b></color>: Unconnected ServerService {type.Name} unregistered.");
                }
                else
                {
                    Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ServerService {type.Name} was not registered.");
                }
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
            (bool validMember, ulong methodId, MemberInfo member, RpcAttribute attr) = IsValidRpc(methodName, RpcMemberType.Method);

            if (validMember)
            {
                SendToLobbyClientServices(ReservedPacketBuilder.Rpc(methodId, member, args), attr.TransportMethod, exception);
            }
        }

        public void SetOnLobbyClientServices<T>(string variableName, T value)
        {
            SetOnLobbyClientServices(variableName, value, null);
        }

        public void SetOnLobbyClientServices<T>(string variableName, T value, UserData exception = null)
        {
            (bool validMember, ulong variableId, MemberInfo member, RpcAttribute attr) = IsValidRpc(variableName, RpcMemberType.Variable);

            if (validMember)
            {
                SendToLobbyClientServices(ReservedPacketBuilder.Rpc(variableId, member, value), attr.TransportMethod, exception);
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
            (bool validMember, ulong methodId, MemberInfo member, RpcAttribute attr) = IsValidRpc(methodName, RpcMemberType.Method);

            if (validMember)
            {
                SendToGameClientServices(ReservedPacketBuilder.Rpc(methodId, member, args), attr.TransportMethod, exception);
            }
        }

        public void SetOnGameClientServices<T>(string variableName, T value)
        {
            SetOnGameClientServices(variableName, value, null);
        }

        public void SetOnGameClientServices<T>(string variableName, T value, UserData exception = null)
        {
            (bool validMember, ulong variableId, MemberInfo member, RpcAttribute attr) = IsValidRpc(variableName, RpcMemberType.Variable);

            if (validMember)
            {
                SendToGameClientServices(ReservedPacketBuilder.Rpc(variableId, member, value), attr.TransportMethod, exception);
            }
        }

        public void SendToUserClientService(UserData user, NetPacket packet, TransportMethod transportMethod)
        {
            lobby.SendToUser(user, serviceId, packet, transportMethod);
        }

        public void InvokeOnUserClientService(UserData user, string methodName, params object[] args)
        {
            (bool validMember, ulong methodId, MemberInfo member, RpcAttribute attr) = IsValidRpc(methodName, RpcMemberType.Method);

            if (validMember)
            {
                SendToUserClientService(user, ReservedPacketBuilder.Rpc(methodId, member, args), attr.TransportMethod);
            }
        }

        public void SetOnUserClientService<T>(UserData user, string variableName, T value)
        {
            (bool validMember, ulong variableId, MemberInfo member, RpcAttribute attr) = IsValidRpc(variableName, RpcMemberType.Variable);

            if (validMember)
            {
                SendToUserClientService(user, ReservedPacketBuilder.Rpc(variableId, member, value), attr.TransportMethod);
            }
        }

        public void SendToUnconnectedClientService(IPEndPoint endPoint, NetPacket packet)
        {
            lobby.SendToUnconnected(endPoint, serviceId, packet);
        }
    }
}