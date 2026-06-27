using System;

namespace Monstroe.CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ClientRpcAttribute : RpcAttribute
    {
        public ClientRpcAttribute(TransportMethod transportMethod = TransportMethod.Reliable) : base(transportMethod) { }
    }
}