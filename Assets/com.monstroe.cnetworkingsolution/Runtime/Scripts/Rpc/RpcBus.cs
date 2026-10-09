using System;
using System.Collections.Generic;
using System.Reflection;

namespace Monstroe.CNetworkingSolution
{
    public class RpcBus
    {
        public sealed class RpcMemberInfo
        {
            public ulong MemberId;
            public MemberInfo Member;
            public RpcAttribute Attribute;
            public RpcMemberType MemberType => Member switch
            {
                MethodInfo _ => RpcMemberType.Method,
                FieldInfo _ => RpcMemberType.Variable,
                PropertyInfo _ => RpcMemberType.Variable,
                _ => throw new Exception($"Invalid RPC member type: {Member.GetType().FullName}")
            };
        }

        private readonly Dictionary<INetRpc, Dictionary<ulong, RpcMemberInfo>> instanceRpcMemberMap = new Dictionary<INetRpc, Dictionary<ulong, RpcMemberInfo>>();
        private readonly Dictionary<Type, Dictionary<string, RpcMemberInfo>> rpcMemberByTypeAndSignature = new Dictionary<Type, Dictionary<string, RpcMemberInfo>>();
        private readonly Dictionary<Type, List<RpcMemberInfo>> rpcMemberCache = new Dictionary<Type, List<RpcMemberInfo>>();

        public void RegisterRpcFieldContainer(INetRpc instance)
        {
            var type = instance.GetType();
        }

        public void RegisterRpcContainer(INetRpc instance)
        {
            var type = instance.GetType();

            if (!rpcMemberCache.TryGetValue(type, out var members))
            {
                members = new List<RpcMemberInfo>();
                var signatureMap = new Dictionary<string, RpcMemberInfo>();

                foreach (var member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (member.GetCustomAttribute<RpcAttribute>() is RpcAttribute attr)
                    {
                        if (signatureMap.ContainsKey(member.Name))
                        {
                            throw new Exception($"Overloaded RPC members are not allowed: {type.Name}.{member.Name}");
                        }

                        switch (member)
                        {
                            case MethodInfo m:
                                {
                                    if (m.ReturnType != typeof(void))
                                    {
                                        throw new Exception($"Method {type.Name}.{m.Name} has invalid return type for RPC. Only void return type is allowed.");
                                    }

                                    ParameterInfo[] parameters = m.GetParameters();
                                    foreach (var p in parameters)
                                    {
                                        if (!NetPacket.IsSupportedType(p.ParameterType))
                                        {
                                            throw new Exception($"Method {type.Name}.{member.Name} has unsupported parameter type {p.ParameterType.FullName} for RPC.");
                                        }

                                        if (p.GetCustomAttribute<RpcSenderAttribute>() != null && p.ParameterType != typeof(UserData))
                                        {
                                            throw new Exception($"Method {type.Name}.{member.Name} has invalid RpcSender parameter {p.Name}. Only UserData type is allowed for RpcSender.");
                                        }
                                    }
                                    break;
                                }
                            case FieldInfo f:
                                {
                                    if (!NetPacket.IsSupportedType(f.FieldType))
                                    {
                                        throw new Exception($"Field {type.Name}.{f.Name} has unsupported type {f.FieldType.FullName} for RPC.");
                                    }
                                    break;
                                }
                            case PropertyInfo p:
                                {
                                    if (!NetPacket.IsSupportedType(p.PropertyType))
                                    {
                                        throw new Exception($"Property {type.Name}.{p.Name} has unsupported type {p.PropertyType.FullName} for RPC.");
                                    }
                                    break;
                                }
                            default:
                                throw new Exception($"Member {type.Name}.{member.Name} is not a valid RPC member. Only methods, fields, and properties are allowed.");
                        }

                        ulong id = NetResources.GenerateHashKey(member.Name);

                        RpcMemberInfo rpcMember = new RpcMemberInfo
                        {
                            MemberId = id,
                            Member = member,
                            Attribute = attr
                        };

                        members.Add(rpcMember);
                        signatureMap[member.Name] = rpcMember;
                    }
                }
                rpcMemberCache[type] = members;
                rpcMemberByTypeAndSignature[type] = signatureMap;
            }

            var methodMap = new Dictionary<ulong, RpcMemberInfo>();
            foreach (RpcMemberInfo member in members)
            {
                methodMap[member.MemberId] = member;
            }

            instanceRpcMemberMap[instance] = methodMap;
        }

        public void UnregisterRpcContainer(INetRpc instance)
        {
            instanceRpcMemberMap.Remove(instance);
        }

        public bool TryGetRpcMemberByInstanceAndId(INetRpc instance, ulong memberId, out MemberInfo member)
        {
            var rpcMemberInfo = instanceRpcMemberMap.TryGetValue(instance, out var members) && members.TryGetValue(memberId, out var info) ? info : null;
            member = rpcMemberInfo?.Member;
            return member != null;
        }

        public bool TryGetRpcMemberByTypeAndName(Type type, string memberName, out ulong memberId, out MemberInfo member, out RpcMemberType memberType, out RpcAttribute attribute)
        {
            var rpcMember = rpcMemberByTypeAndSignature.TryGetValue(type, out var map) && map.TryGetValue(memberName, out var info) ? info : null;
            memberId = rpcMember?.MemberId ?? 0;
            member = rpcMember?.Member;
            memberType = rpcMember?.MemberType ?? RpcMemberType.Method;
            attribute = rpcMember?.Attribute;
            return rpcMember != null;
        }
    }

    public enum RpcMemberType
    {
        Method,
        Variable
    }
}