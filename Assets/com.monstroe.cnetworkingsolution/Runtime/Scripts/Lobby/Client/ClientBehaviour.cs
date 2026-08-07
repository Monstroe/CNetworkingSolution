using System;
using System.Net;
using System.Reflection;
using UnityEngine;

namespace Monstroe.CNetworkingSolution
{
    public abstract class ClientBehaviour : MonoBehaviour, INetRpc
    {
        protected ClientLobby lobby;
        protected Type type;

        public virtual void Init(ClientLobby lobby)
        {
            this.lobby = lobby;
            this.type = GetType();
            lobby.RegisterRpcContainer(this);
        }

        public virtual void Remove()
        {
            lobby.UnregisterRpcContainer(this);
        }

        internal bool ReceiveData(NetPacket packet, ulong serviceId, ushort commandType, TransportMethod? transportMethod)
        {
            bool packetHandled = true;
            if ((ReservedCommandType)commandType == ReservedCommandType.RPC)
            {
                ulong methodId = packet.ReadULong();
                if (lobby.RpcBus.TryGetRpcMethodByInstanceAndId(this, methodId, out MethodInfo method) && method.GetCustomAttribute<ServerRpcAttribute>() == null)
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
                    Debug.LogError($"<color=red><b>CNS</b></color>: RPC Method with ID {methodId} not found on ClientBehaviour {type.Name}.");
                }
            }
            else
            {
                packetHandled = ReceiveData(packet, commandType, transportMethod);
                if (!packetHandled)
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Unhandled packet received on ClientBehaviour {type.Name} with service ID {serviceId} and command type {commandType}.");
                }
            }

            return packetHandled;
        }

        internal bool ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ulong serviceId, ushort commandType)
        {
            bool packetHandled = ReceiveDataUnconnected(ipEndPoint, packet, commandType);
            if (!packetHandled)
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: Unhandled unconnected packet received on ClientBehaviour {type.Name} with service ID {serviceId} and command type {commandType}.");
            }

            return packetHandled;
        }

        public virtual bool ReceiveData(NetPacket packet, ushort commandType, TransportMethod? transportMethod) { return false; }

        public virtual bool ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ushort commandType) { return false; }

        protected void InstantiateOnNetwork(string originalPath, Vector3 position, Quaternion rotation)
        {
            SendObjectSpawnRequest(originalPath, position, rotation);
        }

        protected void InstantiateOnNetwork(GameObject original, Vector3 position, Quaternion rotation)
        {
            original.TryGetComponent(out ClientObject clientObject);
            if (clientObject != null)
            {
                SendObjectSpawnRequest(clientObject.PrefabPath, position, rotation);
            }
            else
            {
                Debug.LogError("ClientBehavior InstantiateOnNetwork could not find ClientObject component on given GameObject.");
            }
        }

        private void SendObjectSpawnRequest(string originalPath, Vector3 position, Quaternion rotation)
        {
            if (NetResources.Instance.GetClientPrefabKeyFromPath(originalPath) == 0)
            {
                Debug.LogError("ClientBehaviour SendObjectSpawnRequest could not find client prefab key for path: " + originalPath);
                return;
            }

            lobby.SendToServer<ObjectClientService>(ObjectPacketBuilder.ObjectSpawnRequest(originalPath, position, rotation), TransportMethod.Reliable);
        }

        protected void DestroyOnNetwork(ClientObject clientObj)
        {
            if (clientObj.OwnerId == lobby.CurrentUser.PlayerId)
            {
                lobby.SendToServer<ObjectClientService>(ObjectPacketBuilder.ObjectDestroyRequest(clientObj.Id), TransportMethod.Reliable);
            }
            else
            {
                Debug.LogError("ClientBehaviour DestroyOnNetwork attempted to destroy an object not owned by the current user.");
            }
        }
    }
}