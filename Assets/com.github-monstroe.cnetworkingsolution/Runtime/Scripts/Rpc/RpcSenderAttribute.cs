using System;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class RpcSenderAttribute : Attribute
{
    public RpcSenderAttribute() { }
}
