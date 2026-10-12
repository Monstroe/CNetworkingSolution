using System;

namespace Monstroe.CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ClientRpcAttribute : RpcAttribute
    {
        public bool InvokeFromClientOnly { get; }

        public ClientRpcAttribute(bool invokeFromClientOnly = true, TransportMethod transportMethod = TransportMethod.Reliable) : base(transportMethod)
        {
            InvokeFromClientOnly = invokeFromClientOnly;
        }
    }
}