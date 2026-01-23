using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RpcAttribute : Attribute
{
    public TransportMethod TransportMethod { get; }

    public RpcAttribute(TransportMethod transportMethod = TransportMethod.Reliable)
    {
        TransportMethod = transportMethod;
    }
}