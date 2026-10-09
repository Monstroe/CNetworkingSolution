using System;

namespace Monstroe.CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RpcAttribute : Attribute
    {
        public TransportMethod TransportMethod { get; }

        public RpcAttribute(TransportMethod transportMethod = TransportMethod.Reliable)
        {
            TransportMethod = transportMethod;
        }
    }
}