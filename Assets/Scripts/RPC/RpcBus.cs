using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
public class RpcBus
{
    private class RpcMethodInfo
    {
        public ushort MethodId;
        public string MethodSignature;
        public MethodInfo Method;
    }

    private readonly Dictionary<object, Dictionary<ushort, RpcMethodInfo>> instanceRpcMethodMap = new Dictionary<object, Dictionary<ushort, RpcMethodInfo>>();
    private readonly Dictionary<Type, Dictionary<string, ushort>> rpcMethodIdByTypeAndSignature = new Dictionary<Type, Dictionary<string, ushort>>();
    private readonly Dictionary<Type, List<RpcMethodInfo>> rpcMethodCache = new Dictionary<Type, List<RpcMethodInfo>>();

    public void RegisterRpcContainer(object instance)
    {
        var type = instance.GetType();

        if (!rpcMethodCache.TryGetValue(type, out var methods))
        {
            methods = new List<RpcMethodInfo>();
            var signatureMap = new Dictionary<string, ushort>();
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<RpcAttribute>() is RpcAttribute attr)
                {
                    if (method.ReturnType != typeof(void))
                    {
                        throw new Exception($"Method {type.Name}.{method.Name} has invalid return type for RPC.");
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    foreach (var p in parameters)
                    {
                        if (!NetPacket.IsSupportedType(p.ParameterType))
                        {
                            throw new Exception($"Method {type.Name}.{method.Name} has unsupported parameter type {p.ParameterType.FullName} for RPC.");
                        }
                    }

                    ushort id = GenerateRpcMethodId(method);

                    RpcMethodInfo rpcMethod = new RpcMethodInfo
                    {
                        MethodId = id,
                        MethodSignature = GenerateRpcMethodSignature(method),
                        Method = method
                    };

                    methods.Add(rpcMethod);
                    signatureMap[rpcMethod.MethodSignature] = id;
                }
            }
            rpcMethodCache[type] = methods;
            rpcMethodIdByTypeAndSignature[type] = signatureMap;
        }

        var methodMap = new Dictionary<ushort, RpcMethodInfo>();
        foreach (RpcMethodInfo method in methods)
        {

            methodMap[method.MethodId] = method;
        }

        instanceRpcMethodMap[instance] = methodMap;
    }

    public void UnregisterRpcContainer(object instance)
    {
        instanceRpcMethodMap.Remove(instance);
    }

    public MethodInfo GetRpcMethod(object instance, ushort methodId)
    {
        return instanceRpcMethodMap[instance][methodId].Method;
    }

    public ushort GetRpcMethodId(Type type, MethodInfo method)
    {
        return rpcMethodIdByTypeAndSignature[type][GenerateRpcMethodSignature(method)];
    }

    public ushort GenerateRpcMethodId(MethodInfo method)
    {
        string signature = GenerateRpcMethodSignature(method);

        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(signature));

        return BitConverter.ToUInt16(hash, 0);
    }

    private string GenerateRpcMethodSignature(MethodInfo method)
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
}