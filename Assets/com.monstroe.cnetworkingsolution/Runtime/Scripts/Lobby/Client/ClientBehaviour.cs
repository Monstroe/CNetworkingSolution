using System;
using System.Net;
using System.Reflection;
using UnityEngine;

namespace Monstroe.CNetworkingSolution
{
    public abstract class ClientBehaviour : NetBehavior, INetRpc
    {
        protected ClientLobby lobby;

        public virtual void Init(ClientLobby lobby)
        {
            base.Init(lobby);
            this.lobby = lobby;
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
                ulong memberId = packet.ReadULong();
                bool validMember = lobby.RpcBus.TryGetRpcMemberByInstanceAndId(this, memberId, out MemberInfo member);
                ClientRpcAttribute attr = member?.GetCustomAttribute<ClientRpcAttribute>();
                if (validMember && attr != null)
                {
                    if (member is MethodInfo method)
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        object[] args = new object[parameters.Length];
                        for (int i = 0; i < args.Length; i++)
                        {
                            args[i] = packet.ReadObject(parameters[i].ParameterType);
                        }

                        method.Invoke(this, args);
                    }
                    else if (attr is ClientVarRpcAttribute)
                    {
                        Type memberType;
                        object currentValue;

                        switch (member)
                        {
                            case FieldInfo field:
                                memberType = field.FieldType;
                                currentValue = field.GetValue(this);
                                break;

                            case PropertyInfo property:
                                memberType = property.PropertyType;
                                currentValue = property.GetValue(this);
                                break;

                            default:
                                throw new ArgumentException($"RPC Member with ID {memberId} is not a valid RPC member on ClientBehaviour {type.Name}.");
                        }

                        if (currentValue is INetSerializable serializableValue)
                        {
                            // Use the DeserializeInto method to populate the existing instance of the member, so that we don't create a new instance and lose the reference
                            serializableValue.DeserializeInto(packet);
                        }
                        else
                        {
                            object value = packet.ReadObject(memberType);
                            switch (member)
                            {
                                case FieldInfo field:
                                    field.SetValue(this, value);
                                    break;

                                case PropertyInfo property:
                                    property.SetValue(this, value);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"<color=red><b>CNS</b></color>: Non-method RPC Member with ID {memberId} on ClientBehaviour {type.Name} does not have a valid ClientVarRpcAttribute");
                    }
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: RPC Member with ID {memberId} not found on ClientBehaviour {type.Name}");
                }
            }
            else
            {
                packetHandled = ReceiveData(packet, commandType, transportMethod);
                if (!packetHandled)
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Unhandled packet received on ClientBehaviour {type.Name} with service ID {serviceId} and command type {commandType}");
                }
            }

            return packetHandled;
        }

        internal bool ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ulong serviceId, ushort commandType)
        {
            bool packetHandled = ReceiveDataUnconnected(ipEndPoint, packet, commandType);
            if (!packetHandled)
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: Unhandled unconnected packet received on ClientBehaviour {type.Name} with service ID {serviceId} and command type {commandType}");
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
                Debug.LogError("<color=red><b>CNS</b></color>: ClientBehavior InstantiateOnNetwork could not find ClientObject component on given GameObject");
            }
        }

        private void SendObjectSpawnRequest(string originalPath, Vector3 position, Quaternion rotation)
        {
            if (NetResources.Instance.GetClientPrefabKeyFromPath(originalPath) == 0)
            {
                Debug.LogError("<color=red><b>CNS</b></color>: ClientBehaviour SendObjectSpawnRequest could not find client prefab key for path: " + originalPath);
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
                Debug.LogError("<color=red><b>CNS</b></color>: ClientBehaviour DestroyOnNetwork attempted to destroy an object not owned by the current user");
            }
        }
    }
}