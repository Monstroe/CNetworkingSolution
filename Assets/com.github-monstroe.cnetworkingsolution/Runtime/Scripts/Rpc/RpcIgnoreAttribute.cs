using System;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class RpcIgnoreAttribute : Attribute
{
    public RpcIgnoreAttribute() { }
}
