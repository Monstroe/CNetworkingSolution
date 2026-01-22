using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RpcAttribute : Attribute
{
    public RpcAttribute() { }
}