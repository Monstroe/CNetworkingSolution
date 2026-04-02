using System;
using System.Collections.Generic;
using System.Reflection;

public class IdBus
{
    public sealed class RpcMethodInfo
    {
        public ulong MethodId;
        public MethodInfo Method;
        public RpcAttribute Attribute;
    }

    private readonly Dictionary<INetRpc, Dictionary<ulong, RpcMethodInfo>> instanceRpcMethodMap = new Dictionary<INetRpc, Dictionary<ulong, RpcMethodInfo>>();
    private readonly Dictionary<Type, Dictionary<string, RpcMethodInfo>> rpcMethodByTypeAndSignature = new Dictionary<Type, Dictionary<string, RpcMethodInfo>>();
    private readonly Dictionary<Type, List<RpcMethodInfo>> rpcMethodCache = new Dictionary<Type, List<RpcMethodInfo>>();

    public void RegisterRpcContainer(INetRpc instance)
    {
        var type = instance.GetType();

        if (!rpcMethodCache.TryGetValue(type, out var methods))
        {
            methods = new List<RpcMethodInfo>();
            var signatureMap = new Dictionary<string, RpcMethodInfo>();
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<RpcAttribute>() is RpcAttribute attr)
                {
                    if (method.ReturnType != typeof(void))
                    {
                        throw new Exception($"Method {type.Name}.{method.Name} has invalid return type for RPC.");
                    }

                    if (signatureMap.ContainsKey(method.Name))
                    {
                        throw new Exception($"Overloaded RPC methods are not allowed: {type.Name}.{method.Name}");
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    foreach (var p in parameters)
                    {
                        if (!NetPacket.IsSupportedType(p.ParameterType))
                        {
                            throw new Exception($"Method {type.Name}.{method.Name} has unsupported parameter type {p.ParameterType.FullName} for RPC.");
                        }
                    }

                    ulong id = NetResources.GenerateHashKey(method.Name);

                    RpcMethodInfo rpcMethod = new RpcMethodInfo
                    {
                        MethodId = id,
                        Method = method,
                        Attribute = attr
                    };

                    methods.Add(rpcMethod);
                    signatureMap[method.Name] = rpcMethod;
                }
            }
            rpcMethodCache[type] = methods;
            rpcMethodByTypeAndSignature[type] = signatureMap;
        }

        var methodMap = new Dictionary<ulong, RpcMethodInfo>();
        foreach (RpcMethodInfo method in methods)
        {

            methodMap[method.MethodId] = method;
        }

        instanceRpcMethodMap[instance] = methodMap;
    }

    public void UnregisterRpcContainer(INetRpc instance)
    {
        instanceRpcMethodMap.Remove(instance);
    }

    public bool TryGetRpcMethodByInstanceAndId(INetRpc instance, ulong methodId, out MethodInfo method)
    {
        var rpcMethodInfo = instanceRpcMethodMap.TryGetValue(instance, out var methods) && methods.TryGetValue(methodId, out var info) ? info : null;
        method = rpcMethodInfo?.Method;
        return method != null;
    }

    public bool TryGetRpcMethodByTypeAndName(Type type, string methodName, out ulong methodId, out MethodInfo method, out RpcAttribute attribute)
    {
        var rpcMethod = rpcMethodByTypeAndSignature.TryGetValue(type, out var map) && map.TryGetValue(methodName, out var info) ? info : null;
        methodId = rpcMethod?.MethodId ?? 0;
        method = rpcMethod?.Method;
        attribute = rpcMethod?.Attribute;
        return rpcMethod != null;
    }
}