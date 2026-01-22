using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ClientRpcAttribute : Attribute
{
    public ClientRpcAttribute() { }
}