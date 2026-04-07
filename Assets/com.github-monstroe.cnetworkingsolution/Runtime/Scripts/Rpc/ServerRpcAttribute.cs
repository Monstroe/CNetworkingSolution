using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ServerRpcAttribute : RpcAttribute
{
    public ServerRpcAttribute(TransportMethod transportMethod = TransportMethod.Reliable) : base(transportMethod) { }
}