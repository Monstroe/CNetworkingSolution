using System;

namespace Monstroe.CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ServerRpcAttribute : RpcAttribute
    {
        public ServerRpcAttribute(TransportMethod transportMethod = TransportMethod.Reliable) : base(transportMethod) { }
    }
}