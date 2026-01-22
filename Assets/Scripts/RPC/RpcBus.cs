using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

public static class RpcBus
{
    private class RpcMethod
    {
        public ushort Id;
        public MethodInfo Method;
        public ParameterInfo[] Parameters;
    }

    private static readonly Dictionary<Type, Dictionary<ushort, RpcMethod>> methodsByType = new Dictionary<Type, Dictionary<ushort, RpcMethod>>();
    private static readonly Dictionary<Type, Dictionary<string, ushort>> idBySignature = new Dictionary<Type, Dictionary<string, ushort>>();

    public static void RegisterRpcContainer(Type type)
    {
        if (methodsByType.ContainsKey(type))
        {
            return;
        }

        var methodMap = new Dictionary<ushort, RpcMethod>();
        var sigMap = new Dictionary<string, ushort>();

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.GetCustomAttribute<RpcAttribute>() is RpcAttribute attr)
            {
                if (method.ReturnType != typeof(void))
                {
                    throw new Exception($"Method {type.Name}.{method.Name} must return void for RPC.");
                }

                foreach (var p in method.GetParameters())
                {
                    if (!NetSerializer.CanSerialize(p.ParameterType))
                    {
                        throw new Exception($"Method {type.Name}.{method.Name} has unserializable parameter {p.ParameterType.FullName} for RPC.");
                    }
                }

                ushort id = GenerateMethodId(type, method);

                var rpc = new RpcMethod
                {
                    Id = id,
                    Method = method,
                    Parameters = method.GetParameters(),
                    Target = attr.Target
                };

                methodMap[id] = rpc;
                sigMap[GenerateMethodSignature(method)] = id;
            }
        }

        methodsByType[type] = methodMap;
        idBySignature[type] = sigMap;
    }

    private static RpcMethod GetRpc(Type type, ushort id)
    {
        return methodsByType[type][id];
    }

    private static ushort GetMethodId(Type type, MethodInfo method)
    {
        return idBySignature[type][GenerateMethodSignature(method)];
    }

    private static ushort GenerateMethodId(Type type, MethodInfo method)
    {
        string signature = type.FullName + "." + GenerateMethodSignature(method);

        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(signature));

        return BitConverter.ToUInt16(hash, 0);
    }

    private static string GenerateMethodSignature(MethodInfo method)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(method.Name);
        sb.Append("(");

        ParameterInfo[] parameters = method.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            sb.Append(parameters[i].ParameterType.FullName);
            if (i < parameters.Length - 1)
                sb.Append(",");
        }

        sb.Append(")");
        return sb.ToString();
    }

    private static void ValidateRpcMethod(MethodInfo method)
    {
        if (method.ReturnType != typeof(void))
        {
            throw new Exception($"{method.DeclaringType.Name}.{method.Name} must return void for RPC.");
        }

        foreach (var p in method.GetParameters())
        {
            if (!NetSerializer.CanSerialize(p.ParameterType))
            {
                throw new Exception(
                    $"RPC {method.Name} has unserializable parameter {p.ParameterType.Name}");
            }
        }
    }
}