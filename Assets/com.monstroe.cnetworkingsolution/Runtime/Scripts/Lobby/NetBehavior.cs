using System;
using System.Reflection;
using UnityEngine;

namespace Monstroe.CNetworkingSolution
{
    public abstract class NetBehavior : MonoBehaviour
    {
        private NetLobby lobby;
        protected Type type;

        public void Init(NetLobby lobby)
        {
            this.lobby = lobby;
            this.type = GetType();
        }

        protected (bool, ulong, MemberInfo, RpcAttribute) IsValidRpc(string memberName, RpcMemberType expectedType)
        {
            bool validMember = lobby.RpcBus.TryGetRpcMemberByTypeAndName(type, memberName, out ulong memberId, out MemberInfo member, out RpcMemberType memberType, out RpcAttribute attr);
            if (!validMember)
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: RPC Attribute not found on Member {type.Name}.{memberName}");
                return (false, 0, null, null);
            }

            bool isMethod = memberType == expectedType;
            if (!isMethod)
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: RPC Member {type.Name}.{memberName} is not a valid {expectedType}");
                return (false, 0, null, null);
            }

            return (true, memberId, member, attr);
        }
    }
}
