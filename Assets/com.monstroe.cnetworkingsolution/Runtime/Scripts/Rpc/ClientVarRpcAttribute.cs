using System;

namespace Monstroe.CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ClientVarRpcAttribute : ClientRpcAttribute
    {
        public ClientVarRpcAttribute(bool invokeFromClientOnly = true, TransportMethod transportMethod = TransportMethod.Reliable) : base(invokeFromClientOnly, transportMethod) { }
    }
}