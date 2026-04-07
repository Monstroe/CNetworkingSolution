using System;

namespace CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class RpcSenderAttribute : Attribute
    {
        public RpcSenderAttribute() { }
    }
}