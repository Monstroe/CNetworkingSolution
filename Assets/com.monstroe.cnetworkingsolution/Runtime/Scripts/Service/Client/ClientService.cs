using System;
using System.Net;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Monstroe.CNetworkingSolution
{
    public abstract class ClientService : ClientBehaviour
    {
        public ulong ServiceId => serviceId;

        [SerializeField, HideInInspector]
        private ulong serviceId;

        [Header("ClientService Settings")]
        [SerializeField] protected bool registerUnconnectedService = false;

        public override void Init(ClientLobby lobby)
        {
            base.Init(lobby);
            if (lobby.RegisterService(this, out serviceId))
            {
                Debug.Log($"<color=green><b>CNS</b></color>: ClientService {type.Name} registered.");
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {type.Name} is already registered.");
            }

            if (registerUnconnectedService)
            {
                if (lobby.RegisterUnconnectedService(this, out _))
                {
                    Debug.Log($"<color=green><b>CNS</b></color>: Unconnected ClientService {type.Name} registered.");
                }
                else
                {
                    Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ClientService {type.Name} is already registered.");
                }
            }
        }

        public override void Remove()
        {
            if (lobby.UnregisterService(this))
            {
                Debug.Log($"<color=green><b>CNS</b></color>: ClientService {type.Name} unregistered.");
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {type.Name} was not registered.");
            }

            if (registerUnconnectedService)
            {
                if (lobby.UnregisterUnconnectedService(this))
                {
                    Debug.Log($"<color=green><b>CNS</b></color>: Unconnected ClientService {type.Name} unregistered.");
                }
                else
                {
                    Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ClientService {type.Name} was not registered.");
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

        public void SendToServerService(NetPacket packet, TransportMethod transportMethod)
        {
            lobby.SendToServer(serviceId, packet, transportMethod);
        }

        public void InvokeOnServerService(string methodName, params object[] args)
        {
            if (lobby.RpcBus.TryGetRpcMethodByTypeAndName(type, methodName, out ulong methodId, out MethodInfo method, out RpcAttribute attr))
            {
                SendToServerService(ReservedPacketBuilder.Rpc(methodId, method, args), attr.TransportMethod);
            }
            else
            {
                Debug.LogError($"RPC Attribute not found on Method {type.Name}.{methodName}.");
            }
        }

        public void SendToUnconnectedServerService(IPEndPoint endPoint, NetPacket packet)
        {
            lobby.SendToUnconnected(endPoint, serviceId, packet);
        }
    }
}