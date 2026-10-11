using UnityEngine;

namespace Monstroe.CNetworkingSolution
{
    public class NetLobby : MonoBehaviour
    {
        public LobbyData LobbyData { get; internal set; } = new LobbyData();
        internal RpcBus RpcBus { get; } = new RpcBus();

        public void RegisterRpcContainer(INetRpc rpcContainer)
        {
            RpcBus.RegisterRpcContainer(rpcContainer);
        }

        public void UnregisterRpcContainer(INetRpc rpcContainer)
        {
            RpcBus.UnregisterRpcContainer(rpcContainer);
        }
    }
}
