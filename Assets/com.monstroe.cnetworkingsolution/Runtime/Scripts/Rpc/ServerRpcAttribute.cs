using System;

namespace Monstroe.CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ServerRpcAttribute : RpcAttribute
    {
        public bool InvokeFromServerOnly { get; }

        public ServerRpcAttribute(bool invokeFromServerOnly = false, TransportMethod transportMethod = TransportMethod.Reliable) : base(transportMethod)
        {
            InvokeFromServerOnly = invokeFromServerOnly;
        }
    }
}