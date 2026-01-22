using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class RpcServerService : ServerService
{
    private class RpcMethod
    {
        public object Instance;
        public ushort MethodId;
        public MethodInfo Method;
        public RpcParameter[] Parameters;
    }

    private class RpcParameter
    {
        public ParameterInfo Info;
        public MethodInfo SerializeMethod;
        public MethodInfo DeserializeMethod;
    }

    private readonly Dictionary<Type, Dictionary<ushort, RpcMethod>> rpcMethodByTypeAndId = new Dictionary<Type, Dictionary<ushort, RpcMethod>>();
    private readonly Dictionary<Type, Dictionary<string, ushort>> rpcMethodIdByTypeAndSignature = new Dictionary<Type, Dictionary<string, ushort>>();
    private readonly Dictionary<Type, List<MethodInfo>> rpcMethodCache = new Dictionary<Type, List<MethodInfo>>();

    public void RegisterRpcContainer(object instance)
    {
        var type = instance.GetType();

        if (!rpcMethodCache.TryGetValue(type, out var methods))
        {
            methods = new List<MethodInfo>();
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<RpcAttribute>() is RpcAttribute attr)
                {
                    var parameters = method.GetParameters();
                    if (method.ReturnType != typeof(void))
                    {
                        throw new Exception($"Method {type.Name}.{method.Name} has invalid return type for RPC.");
                    }
                    methods.Add(method);
                }
            }
            rpcMethodCache[type] = methods;
        }

        if (rpcMethodByTypeAndId.ContainsKey(type))
        {
            return;
        }

        var methodMap = new Dictionary<ushort, RpcMethod>();
        var signatureMap = new Dictionary<string, ushort>();

        foreach (var method in methods)
        {
            ParameterInfo[] parameters = method.GetParameters();
            List<RpcParameter> rpcParameters = new List<RpcParameter>();
            foreach (var p in parameters)
            {
                Type t = p.ParameterType;

                RpcParameter rpcParam = new RpcParameter()
                {
                    Info = p
                };

                if (typeof(INetSerializable).IsAssignableFrom(t))
                {
                    rpcParam.SerializeMethod = t.GetMethod("Serialize", BindingFlags.Instance | BindingFlags.Public);
                    rpcParam.DeserializeMethod = t.GetMethod("Deserialize", BindingFlags.Instance | BindingFlags.Public);
                }
                else if (t == typeof(byte))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(byte) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadByte", Type.EmptyTypes);
                }
                else if (t == typeof(byte[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(byte[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadBytes", Type.EmptyTypes);
                }
                else if (t == typeof(sbyte))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(sbyte) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadSByte", Type.EmptyTypes);
                }
                else if (t == typeof(sbyte[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(sbyte[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadSBytes", Type.EmptyTypes);
                }
                else if (t == typeof(bool))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(bool) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadBool", Type.EmptyTypes);
                }
                else if (t == typeof(bool[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(bool[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadBools", Type.EmptyTypes);
                }
                else if (t == typeof(char))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(char) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadChar", Type.EmptyTypes);
                }
                else if (t == typeof(char[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(char[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadChars", Type.EmptyTypes);
                }
                else if (t == typeof(double))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(double) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadDouble", Type.EmptyTypes);
                }
                else if (t == typeof(double[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(double[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadDoubles", Type.EmptyTypes);
                }
                else if (t == typeof(float))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(float) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadFloat", Type.EmptyTypes);
                }
                else if (t == typeof(float[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(float[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadFloats", Type.EmptyTypes);
                }
                else if (t == typeof(int))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(int) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadInt", Type.EmptyTypes);
                }
                else if (t == typeof(int[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(int[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadInts", Type.EmptyTypes);
                }
                else if (t == typeof(long))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(long) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadLong", Type.EmptyTypes);
                }
                else if (t == typeof(long[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(long[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadLongs", Type.EmptyTypes);
                }
                else if (t == typeof(short))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(short) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadShort", Type.EmptyTypes);
                }
                else if (t == typeof(short[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(short[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadShorts", Type.EmptyTypes);
                }
                else if (t == typeof(uint))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(uint) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadUInt", Type.EmptyTypes);
                }
                else if (t == typeof(uint[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(uint[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadUInts", Type.EmptyTypes);
                }
                else if (t == typeof(ulong))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(ulong) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadULong", Type.EmptyTypes);
                }
                else if (t == typeof(ulong[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(ulong[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadULongs", Type.EmptyTypes);
                }
                else if (t == typeof(ushort))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(ushort) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadUShort", Type.EmptyTypes);
                }
                else if (t == typeof(ushort[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(ushort[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadUShorts", Type.EmptyTypes);
                }
                else if (t == typeof(string))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(string) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadString", Type.EmptyTypes);
                }
                else if (t == typeof(string[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(string[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadStrings", Type.EmptyTypes);
                }
                else if (t == typeof(Vector2))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(Vector2) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadVector2", Type.EmptyTypes);
                }
                else if (t == typeof(Vector2[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(Vector2[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadVector2s", Type.EmptyTypes);
                }
                else if (t == typeof(Vector3))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(Vector3) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadVector3", Type.EmptyTypes);
                }
                else if (t == typeof(Vector3[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(Vector3[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadVector3s", Type.EmptyTypes);
                }
                else if (t == typeof(Quaternion))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(Quaternion) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadQuaternion", Type.EmptyTypes);
                }
                else if (t == typeof(Quaternion[]))
                {
                    rpcParam.SerializeMethod = typeof(NetPacket).GetMethod("Write", new Type[] { typeof(Quaternion[]) });
                    rpcParam.DeserializeMethod = typeof(NetPacket).GetMethod("ReadQuaternions", Type.EmptyTypes);
                }
                else
                {
                    throw new Exception($"Method {type.Name}.{method.Name} has unserializable parameter {p.ParameterType.FullName} for RPC.");
                }

                rpcParameters.Add(rpcParam);
            }

            ushort id = GenerateMethodId(type, method);

            var rpcMethod = new RpcMethod
            {
                Instance = instance,
                MethodId = id,
                Method = method,
                Parameters = rpcParameters.ToArray()
            };

            methodMap[id] = rpcMethod;
            signatureMap[GenerateMethodSignature(method)] = id;
        }

        rpcMethodByTypeAndId[type] = methodMap;
        rpcMethodIdByTypeAndSignature[type] = signatureMap;
    }

    private RpcMethod GetRpc(Type type, ushort id)
    {
        return rpcMethodByTypeAndId[type][id];
    }

    public ushort GetMethodId(Type type, MethodInfo method)
    {
        return rpcMethodIdByTypeAndSignature[type][GenerateMethodSignature(method)];
    }

    private ushort GenerateMethodId(Type type, MethodInfo method)
    {
        string signature = type.FullName + "." + GenerateMethodSignature(method);

        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(signature));

        return BitConverter.ToUInt16(hash, 0);
    }

    private string GenerateMethodSignature(MethodInfo method)
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

    public static void Write(NetPacket packet, object value, Type parameterType)
    {
        if (typeof(INetSerializable).IsAssignableFrom(parameterType))
        {
            ((INetSerializable)value).Serialize(packet);
            return;
        }

        switch (value)
        {
            case byte v: packet.Write(v); break;
            case byte[] v: packet.Write(v); break;
            case sbyte v: packet.Write(v); break;
            case sbyte[] v: packet.Write(v); break;
            case bool v: packet.Write(v); break;
            case bool[] v: packet.Write(v); break;
            case char v: packet.Write(v); break;
            case char[] v: packet.Write(v); break;
            case double v: packet.Write(v); break;
            case double[] v: packet.Write(v); break;
            case float v: packet.Write(v); break;
            case float[] v: packet.Write(v); break;
            case int v: packet.Write(v); break;
            case int[] v: packet.Write(v); break;
            case long v: packet.Write(v); break;
            case long[] v: packet.Write(v); break;
            case short v: packet.Write(v); break;
            case short[] v: packet.Write(v); break;
            case uint v: packet.Write(v); break;
            case uint[] v: packet.Write(v); break;
            case ulong v: packet.Write(v); break;
            case ulong[] v: packet.Write(v); break;
            case ushort v: packet.Write(v); break;
            case ushort[] v: packet.Write(v); break;
            case string v: packet.Write(v); break;
            case string[] v: packet.Write(v); break;
            default:
                throw new Exception($"Unsupported RPC parameter type: {parameterType.FullName}");
        }
    }

    public static object Read(NetPacket packet, Type parameterType)
    {
        if (typeof(INetSerializable).IsAssignableFrom(parameterType))
        {
            var instance = Activator.CreateInstance(parameterType);
            MethodInfo deserializeMethod = parameterType.GetMethod("Deserialize", BindingFlags.Instance | BindingFlags.Public);
            return deserializeMethod.Invoke(instance, new object[] { packet });
        }

        if (parameterType == typeof(byte)) return packet.ReadByte();
        if (parameterType == typeof(byte[])) return packet.ReadBytes();
        if (parameterType == typeof(sbyte)) return packet.ReadSByte();
        if (parameterType == typeof(sbyte[])) return packet.ReadSBytes();
        if (parameterType == typeof(bool)) return packet.ReadBool();
        if (parameterType == typeof(bool[])) return packet.ReadBools();
        if (parameterType == typeof(char)) return packet.ReadChar();
        if (parameterType == typeof(char[])) return packet.ReadChars();
        if (parameterType == typeof(double)) return packet.ReadDouble();
        if (parameterType == typeof(double[])) return packet.ReadDoubles();
        if (parameterType == typeof(float)) return packet.ReadFloat();
        if (parameterType == typeof(float[])) return packet.ReadFloats();
        if (parameterType == typeof(int)) return packet.ReadInt();
        if (parameterType == typeof(int[])) return packet.ReadInts();
        if (parameterType == typeof(long)) return packet.ReadLong();
        if (parameterType == typeof(long[])) return packet.ReadLongs();
        if (parameterType == typeof(short)) return packet.ReadShort();
        if (parameterType == typeof(short[])) return packet.ReadShorts();
        if (parameterType == typeof(uint)) return packet.ReadUInt();
        if (parameterType == typeof(uint[])) return packet.ReadUInts();
        if (parameterType == typeof(ulong)) return packet.ReadULong();
        if (parameterType == typeof(ulong[])) return packet.ReadULongs();
        if (parameterType == typeof(ushort)) return packet.ReadUShort();
        if (parameterType == typeof(ushort[])) return packet.ReadUShorts();
        if (parameterType == typeof(string)) return packet.ReadString();
        if (parameterType == typeof(string[])) return packet.ReadStrings();

        throw new Exception($"Unsupported RPC parameter type: {parameterType.FullName}");
    }

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }

    public override void Tick()
    {
        // Nothing
    }

    public override void UserJoined(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserLeft(UserData leftUser)
    {
        // Nothing
    }
}
