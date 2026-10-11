using System;

namespace Monstroe.CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class RpcAttribute : Attribute
    {
        public TransportMethod TransportMethod { get; }

        public RpcAttribute(TransportMethod transportMethod = TransportMethod.Reliable)
        {
            TransportMethod = transportMethod;
        }
    }
}