using System;
using System.Reflection;
using UnityEngine;

public static class NetSerializer
{
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

        if (typeof(INetSerializable[]).IsAssignableFrom(parameterType))
        {
            Type elementType = parameterType.GetElementType();
            int length = packet.ReadInt();
            Array array = Array.CreateInstance(elementType, length);
            for (int i = 0; i < length; i++)
            {
                var instance = Activator.CreateInstance(elementType);
                MethodInfo deserializeMethod = elementType.GetMethod("Deserialize", BindingFlags.Instance | BindingFlags.Public);
                deserializeMethod.Invoke(instance, new object[] { packet });
                array.SetValue(instance, i);
            }
            return array;
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
}
