using System;

namespace Monstroe.CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ClientVarRpcAttribute : ClientRpcAttribute
    {
        public ClientVarRpcAttribute(TransportMethod transportMethod = TransportMethod.Reliable) : base(transportMethod) { }
    }
}