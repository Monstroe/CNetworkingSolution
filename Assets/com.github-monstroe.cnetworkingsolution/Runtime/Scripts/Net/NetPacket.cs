using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

public class NetPacket
{
    public byte[] ByteArray
    {
        get
        {
            return byteList.ToArray();
        }
    }

    public byte[] UnreadByteArray
    {
        get
        {
            return byteList.GetRange(CurrentIndex, UnreadLength).ToArray();
        }
    }

    public int Length
    {
        get
        {
            return byteList.Count;
        }
    }

    public int UnreadLength
    {
        get
        {
            return Length - CurrentIndex;
        }
    }

    public int CurrentIndex { get; set; } = 0;

    private List<byte> byteList;

    private static readonly Dictionary<Type, MethodInfo> writeMethodCache = new();
    private static readonly Dictionary<Type, MethodInfo> readMethodCache = new();

    public NetPacket() : this(new List<byte>())
    {
    }

    public NetPacket(byte[] data) : this(new List<byte>())
    {
        byteList.AddRange(data);
    }

    public NetPacket(ArraySegment<byte> data) : this(new List<byte>())
    {
        byteList.AddRange(data);
    }

    public NetPacket(List<byte> data)
    {
        CurrentIndex = 0;
        this.byteList = data;
    }

    public static bool IsSupportedType(Type type)
    {
        if (type == null)
        {
            return false;
        }

        // Nullable<T>
        Type nullableInner = Nullable.GetUnderlyingType(type);
        if (nullableInner != null)
        {
            return IsSupportedType(nullableInner);
        }

        // Arrays
        if (type.IsArray)
        {
            return IsSupportedType(type.GetElementType());
        }

        // INetSerializable
        if (typeof(INetSerializable).IsAssignableFrom(type))
        {
            return true;
        }

        // Primitives
        if (type.IsPrimitive)
        {
            return true;
        }

        // Unity structs
        if (type == typeof(string)) return true;
        if (type == typeof(Vector2)) return true;
        if (type == typeof(Vector3)) return true;
        if (type == typeof(Quaternion)) return true;

        return false;
    }

    public void CopyTo(int packetIndex, byte[] buffer, int arrayIndex, int count)
    {
        byteList.CopyTo(packetIndex, buffer, arrayIndex, count);
    }

    public void Clear()
    {
        byteList.Clear();
        CurrentIndex = 0;
    }

    public void Remove(int offset, int count)
    {
        byteList.RemoveRange(offset, count);
        CurrentIndex = CurrentIndex > offset ? CurrentIndex - count : CurrentIndex;
    }

    public void Insert(int byteOffset, byte value)
    {
        byteList.Insert(byteOffset, value);
    }

    public void Insert(int byteOffset, byte[] value)
    {
        Insert(byteOffset, value.Length);
        byteList.InsertRange(byteOffset + sizeof(int), value);
    }

    public void Insert(int byteOffset, sbyte value)
    {
        byteList.Insert(byteOffset, (byte)value);
    }

    public void Insert(int byteOffset, sbyte[] value)
    {
        Insert(byteOffset, value.Length);
        byteList.InsertRange(byteOffset + sizeof(int), Array.ConvertAll(value, b => (byte)b));
    }

    public void Insert(int byteOffset, bool value)
    {
        byteList.Insert(byteOffset, (byte)(value ? 1 : 0));
    }

    public void Insert(int byteOffset, bool[] value)
    {
        Insert(byteOffset, value.Length);
        byteList.InsertRange(byteOffset + sizeof(int), Array.ConvertAll(value, b => (byte)(b ? 1 : 0)));
    }

    public void Insert(int byteOffset, char value)
    {
        byteList.Insert(byteOffset, (byte)value);
    }

    public void Insert(int byteOffset, char[] value)
    {
        Insert(byteOffset, value.Length);
        byteList.InsertRange(byteOffset + sizeof(int), Array.ConvertAll(value, b => (byte)b));
    }

    public void Insert(int byteOffset, double value)
    {
        byteList.InsertRange(byteOffset, BitConverter.GetBytes(value));
    }

    public void Insert(int byteOffset, double[] value)
    {
        Insert(byteOffset, value.Length);
        for (int i = 0; i < value.Length * sizeof(double); i += sizeof(double))
        {
            Insert(byteOffset + sizeof(int) + i, value[i]);
        }
    }

    public void Insert(int byteOffset, float value)
    {
        byteList.InsertRange(byteOffset, BitConverter.GetBytes(value));
    }

    public void Insert(int byteOffset, float[] value)
    {
        Insert(byteOffset, value.Length);
        for (int i = 0; i < value.Length * sizeof(float); i += sizeof(float))
        {
            Insert(byteOffset + sizeof(int) + i, value[i]);
        }
    }

    public void Insert(int byteOffset, int value)
    {
        byteList.InsertRange(byteOffset, BitConverter.GetBytes(value));
    }

    public void Insert(int byteOffset, int[] value)
    {
        Insert(byteOffset, value.Length);
        for (int i = 0; i < value.Length * sizeof(int); i += sizeof(int))
        {
            Insert(byteOffset + sizeof(int) + i, value[i]);
        }
    }

    public void Insert(int byteOffset, long value)
    {
        byteList.InsertRange(byteOffset, BitConverter.GetBytes(value));
    }

    public void Insert(int byteOffset, long[] value)
    {
        Insert(byteOffset, value.Length);
        for (int i = 0; i < value.Length * sizeof(long); i += sizeof(long))
        {
            Insert(byteOffset + sizeof(int) + i, value[i]);
        }
    }

    public void Insert(int byteOffset, short value)
    {
        byteList.InsertRange(byteOffset, BitConverter.GetBytes(value));
    }

    public void Insert(int byteOffset, short[] value)
    {
        Insert(byteOffset, value.Length);
        for (int i = 0; i < value.Length * sizeof(short); i += sizeof(short))
        {
            Insert(byteOffset + sizeof(int) + i, value[i]);
        }
    }

    public void Insert(int byteOffset, uint value)
    {
        byteList.InsertRange(byteOffset, BitConverter.GetBytes(value));
    }

    public void Insert(int byteOffset, uint[] value)
    {
        Insert(byteOffset, value.Length);
        for (int i = 0; i < value.Length * sizeof(uint); i += sizeof(uint))
        {
            Insert(byteOffset + sizeof(int) + i, value[i]);
        }
    }

    public void Insert(int byteOffset, ulong value)
    {
        byteList.InsertRange(byteOffset, BitConverter.GetBytes(value));
    }

    public void Insert(int byteOffset, ulong[] value)
    {
        Insert(byteOffset, value.Length);
        for (int i = 0; i < value.Length * sizeof(ulong); i += sizeof(ulong))
        {
            Insert(byteOffset + sizeof(int) + i, value[i]);
        }
    }

    public void Insert(int byteOffset, ushort value)
    {
        byteList.InsertRange(byteOffset, BitConverter.GetBytes(value));
    }

    public void Insert(int byteOffset, ushort[] value)
    {
        Insert(byteOffset, value.Length);
        for (int i = 0; i < value.Length * sizeof(ushort); i += sizeof(ushort))
        {
            Insert(byteOffset + sizeof(int) + i, value[i]);
        }
    }

    public void Insert(int byteOffset, string value)
    {
        Insert(byteOffset, value.Length);
        byteList.InsertRange(byteOffset + sizeof(int), Encoding.ASCII.GetBytes(value));
    }

    public void Insert(int byteOffset, string[] value)
    {
        Insert(byteOffset, value.Length);
        int stringOffset = byteOffset + sizeof(int);
        for (int i = 0; i < value.Length; i++)
        {
            Insert(stringOffset, value[i]);
            stringOffset += value[i].Length + sizeof(int);
        }
    }

    // Command
    public void Insert<T>(int byteOffset, T value) where T : Enum
    {
        Insert(byteOffset, Convert.ToUInt16(value));
    }

    // Unity Structs
    public void Insert(int byteOffset, Vector2 value)
    {
        Insert(byteOffset, value.x);
        Insert(byteOffset + sizeof(float), value.y);
    }

    public void Insert(int byteOffset, Vector2[] value)
    {
        Insert(byteOffset, value.Length);
        for (int i = 0; i < value.Length * sizeof(float) * 2; i += sizeof(float) * 2)
        {
            Insert(byteOffset + sizeof(int) + i, value[i]);
        }
    }

    public void Insert(int byteOffset, Vector3 value)
    {
        Insert(byteOffset, value.x);
        Insert(byteOffset + sizeof(float), value.y);
        Insert(byteOffset + sizeof(float) * 2, value.z);
    }

    public void Insert(int byteOffset, Vector3[] value)
    {
        Insert(byteOffset, value.Length);
        for (int i = 0; i < value.Length * sizeof(float) * 3; i += sizeof(float) * 3)
        {
            Insert(byteOffset + sizeof(int) + i, value[i]);
        }
    }

    public void Insert(int byteOffset, Quaternion value)
    {
        Insert(byteOffset, value.x);
        Insert(byteOffset + sizeof(float), value.y);
        Insert(byteOffset + sizeof(float) * 2, value.z);
        Insert(byteOffset + sizeof(float) * 3, value.w);
    }

    public void Insert(int byteOffset, Quaternion[] value)
    {
        Insert(byteOffset, value.Length);
        for (int i = 0; i < value.Length * sizeof(float) * 4; i += sizeof(float) * 4)
        {
            Insert(byteOffset + sizeof(int) + i, value[i]);
        }
    }

    public void Write(byte value)
    {
        byteList.Add(value);
    }

    public void Write(byte? value)
    {
        if (value.HasValue)
        {
            Write(true);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(byte[] value)
    {
        Write(value.Length);
        byteList.AddRange(value);
    }

    public void Write(byte?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(sbyte value)
    {
        byteList.Add((byte)value);
    }

    public void Write(sbyte? value)
    {
        if (value.HasValue)
        {
            Write(true);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(sbyte[] value)
    {
        Write(value.Length);
        byteList.AddRange(Array.ConvertAll(value, b => (byte)b));
    }

    public void Write(sbyte?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(bool value)
    {
        byteList.Add((byte)(value ? 1 : 0));
    }

    public void Write(bool? value)
    {
        if (value.HasValue)
        {
            Write(true);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(bool[] value)
    {
        Write(value.Length);
        byteList.AddRange(Array.ConvertAll(value, b => (byte)(b ? 1 : 0)));
    }

    public void Write(bool?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(char value)
    {
        byteList.Add((byte)value);
    }

    public void Write(char? value)
    {
        if (value.HasValue)
        {
            Write(true);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(char[] value)
    {
        Write(value.Length);
        byteList.AddRange(Array.ConvertAll(value, b => (byte)b));
    }

    public void Write(char?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(double value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(double? value)
    {
        if (value.HasValue)
        {
            Write(true);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(double[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(double?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(float value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(float? value)
    {
        if (value.HasValue)
        {
            Write(true);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(float[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(float?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(int value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(int? value)
    {
        if (value.HasValue)
        {
            Write(true);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(int[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(int?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(long value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(long? value)
    {
        if (value.HasValue)
        {
            Write(true);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(long[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(long?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(short value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(short? value)
    {
        if (value.HasValue)
        {
            Write(true);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(short[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(short?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(uint value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(uint? value)
    {
        if (value.HasValue)
        {
            Write(true);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(uint[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(uint?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(ulong value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(ulong? value)
    {
        if (value.HasValue)
        {
            Write(true);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(ulong[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(ulong?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(ushort value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(ushort? value)
    {
        if (value.HasValue)
        {
            Write(true);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(ushort[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(ushort?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(string value)
    {
        if (value == null)
        {
            Write(-1);
            return;
        }
        Write(value.Length);
        byteList.AddRange(Encoding.ASCII.GetBytes(value));
    }

    public void Write(string[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    // Command
    public void Write<T>(T value) where T : Enum
    {
        Write(Convert.ToUInt16(value));
    }

    // Unity Structs
    public void Write(Vector2 value)
    {
        Write(value.x);
        Write(value.y);
    }

    public void Write(Vector2? value)
    {
        if (value.HasValue)
        {
            Write(value.HasValue);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(Vector2[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(Vector2?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(Vector3 value)
    {
        Write(value.x);
        Write(value.y);
        Write(value.z);
    }

    public void Write(Vector3? value)
    {
        if (value.HasValue)
        {
            Write(value.HasValue);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(Vector3[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(Vector3?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(Quaternion value)
    {
        Write(value.x);
        Write(value.y);
        Write(value.z);
        Write(value.w);
    }

    public void Write(Quaternion? value)
    {
        if (value.HasValue)
        {
            Write(value.HasValue);
            Write(value.Value);
        }
        else
        {
            Write(false);
        }
    }

    public void Write(Quaternion[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(Quaternion?[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            if (item.HasValue)
            {
                Write(item.HasValue);
                Write(item.Value);
            }
            else
            {
                Write(false);
            }
        }
    }

    public void Write(object value, Type parameterType)
    {
        if (parameterType == null)
        {
            throw new ArgumentNullException(nameof(parameterType));
        }

        // Nullable<T>
        Type nullableInner = Nullable.GetUnderlyingType(parameterType);
        if (nullableInner != null)
        {
            bool hasValue = value != null;
            Write(hasValue);
            if (hasValue)
            {
                Write(value, nullableInner);
            }
            return;
        }

        // Arrays
        if (parameterType.IsArray)
        {
            if (value == null)
            {
                Write(-1);
                return;
            }

            Array array = (Array)value;
            int length = array.Length;
            Write(length);

            Type elementType = parameterType.GetElementType();
            Type elementNullableInner = Nullable.GetUnderlyingType(elementType);
            Type effectiveElementType = elementNullableInner ?? elementType;

            // INetSerializable array
            if (typeof(INetSerializable).IsAssignableFrom(effectiveElementType))
            {
                for (int i = 0; i < length; i++)
                {
                    object element = array.GetValue(i);
                    bool hasElement = element != null;
                    Write(hasElement);

                    if (hasElement)
                    {
                        ((INetSerializable)element).Serialize(this);
                    }
                }

                return;
            }

            // Primitive array
            for (int i = 0; i < length; i++)
            {
                Write(array.GetValue(i), elementType);
            }

            return;
        }

        // INetSerializable
        if (typeof(INetSerializable).IsAssignableFrom(parameterType))
        {
            bool hasValue = value != null;
            Write(hasValue);
            if (hasValue)
            {
                ((INetSerializable)value).Serialize(this);
            }
            return;
        }

        // Primitives
        if (parameterType == typeof(byte)) { Write((byte)value); return; }
        if (parameterType == typeof(byte[])) { Write((byte[])value); return; }
        if (parameterType == typeof(sbyte)) { Write((sbyte)value); return; }
        if (parameterType == typeof(sbyte[])) { Write((sbyte[])value); return; }
        if (parameterType == typeof(bool)) { Write((bool)value); return; }
        if (parameterType == typeof(bool[])) { Write((bool[])value); return; }
        if (parameterType == typeof(char)) { Write((char)value); return; }
        if (parameterType == typeof(char[])) { Write((char[])value); return; }
        if (parameterType == typeof(double)) { Write((double)value); return; }
        if (parameterType == typeof(double[])) { Write((double[])value); return; }
        if (parameterType == typeof(float)) { Write((float)value); return; }
        if (parameterType == typeof(float[])) { Write((float[])value); return; }
        if (parameterType == typeof(int)) { Write((int)value); return; }
        if (parameterType == typeof(int[])) { Write((int[])value); return; }
        if (parameterType == typeof(long)) { Write((long)value); return; }
        if (parameterType == typeof(long[])) { Write((long[])value); return; }
        if (parameterType == typeof(short)) { Write((short)value); return; }
        if (parameterType == typeof(short[])) { Write((short[])value); return; }
        if (parameterType == typeof(uint)) { Write((uint)value); return; }
        if (parameterType == typeof(uint[])) { Write((uint[])value); return; }
        if (parameterType == typeof(ulong)) { Write((ulong)value); return; }
        if (parameterType == typeof(ulong[])) { Write((ulong[])value); return; }
        if (parameterType == typeof(ushort)) { Write((ushort)value); return; }
        if (parameterType == typeof(ushort[])) { Write((ushort[])value); return; }
        if (parameterType == typeof(string)) { Write((string)value); return; }
        if (parameterType == typeof(string[])) { Write((string[])value); return; }
        if (parameterType == typeof(Vector2)) { Write((Vector2)value); return; }
        if (parameterType == typeof(Vector2[])) { Write((Vector2[])value); return; }
        if (parameterType == typeof(Vector3)) { Write((Vector3)value); return; }
        if (parameterType == typeof(Vector3[])) { Write((Vector3[])value); return; }
        if (parameterType == typeof(Quaternion)) { Write((Quaternion)value); return; }
        if (parameterType == typeof(Quaternion[])) { Write((Quaternion[])value); return; }

        throw new Exception($"Unsupported parameter type: {parameterType.FullName}");
    }

    public byte ReadByte(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(byte);
        var value = byteList[CurrentIndex];
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public byte? ReadNullableByte(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        byte value = ReadByte();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(byte) + sizeof(bool);
        return value;
    }

    public byte[] ReadBytes(bool moveIndexPosition = true)
    {
        int length = ReadInt(false);
        var value = byteList.GetRange(CurrentIndex + sizeof(int), length).ToArray();
        CurrentIndex += moveIndexPosition ? length + sizeof(int) : 0;
        return value;
    }

    public byte?[] ReadNullableBytes(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new byte?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableByte();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public sbyte ReadSByte(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(sbyte);
        var value = (sbyte)byteList[CurrentIndex];
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public sbyte? ReadNullableSByte(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        sbyte value = ReadSByte();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(sbyte) + sizeof(bool);
        return value;
    }

    public sbyte[] ReadSBytes(bool moveIndexPosition = true)
    {
        int length = ReadInt(false);
        var value = Array.ConvertAll(byteList.GetRange(CurrentIndex + sizeof(int), length).ToArray(), b => (sbyte)b);
        CurrentIndex += moveIndexPosition ? length + sizeof(int) : 0;
        return value;
    }

    public sbyte?[] ReadNullableSBytes(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new sbyte?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableSByte();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public bool ReadBool(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(bool);
        var value = byteList[CurrentIndex] != 0;
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public bool? ReadNullableBool(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        bool value = ReadBool();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool) + sizeof(bool);
        return value;
    }

    public bool[] ReadBools(bool moveIndexPosition = true)
    {
        int length = ReadInt(false);
        var value = Array.ConvertAll(byteList.GetRange(CurrentIndex + sizeof(int), length).ToArray(), b => b != 0);
        CurrentIndex += moveIndexPosition ? length + sizeof(int) : 0;
        return value;
    }

    public bool?[] ReadNullableBools(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new bool?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableBool();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public char ReadChar(bool moveIndexPosition = true)
    {
        int typeSize = 1;
        var value = (char)byteList[CurrentIndex];
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public char? ReadNullableChar(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        char value = ReadChar();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(char) + sizeof(bool);
        return value;
    }

    public char[] ReadChars(bool moveIndexPosition = true)
    {
        int length = ReadInt(false);
        var value = Array.ConvertAll(byteList.GetRange(CurrentIndex + sizeof(int), length).ToArray(), b => (char)b);
        CurrentIndex += moveIndexPosition ? length + sizeof(int) : 0;
        return value;
    }

    public char?[] ReadNullableChars(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new char?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableChar();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public double ReadDouble(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(double);
        var value = BitConverter.ToDouble(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public double? ReadNullableDouble(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        double value = ReadDouble();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(double) + sizeof(bool);
        return value;
    }

    public double[] ReadDoubles(bool moveIndexPosition = true)
    {
        int length = ReadInt();
        int typeSize = length * sizeof(double) + sizeof(int);
        var value = new double[length];
        for (int i = 0; i < length; i++)
            value[i] = ReadDouble();
        CurrentIndex -= moveIndexPosition ? 0 : typeSize;
        return value;
    }

    public double?[] ReadNullableDoubles(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new double?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableDouble();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public float ReadFloat(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(float);
        var value = BitConverter.ToSingle(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public float? ReadNullableFloat(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        float value = ReadFloat();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(float) + sizeof(bool);
        return value;
    }

    public float[] ReadFloats(bool moveIndexPosition = true)
    {
        int length = ReadInt();
        int typeSize = length * sizeof(float) + sizeof(int);
        var value = new float[length];
        for (int i = 0; i < length; i++)
            value[i] = ReadFloat();
        CurrentIndex -= moveIndexPosition ? 0 : typeSize;
        return value;
    }

    public float?[] ReadNullableFloats(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new float?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableFloat();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public int ReadInt(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(int);
        var value = BitConverter.ToInt32(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public int? ReadNullableInt(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        int value = ReadInt();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(int) + sizeof(bool);
        return value;
    }

    public int[] ReadInts(bool moveIndexPosition = true)
    {
        int length = ReadInt();
        int typeSize = length * sizeof(int) + sizeof(int);
        var value = new int[length];
        for (int i = 0; i < length; i++)
            value[i] = ReadInt();
        CurrentIndex -= moveIndexPosition ? 0 : typeSize;
        return value;
    }

    public int?[] ReadNullableInts(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new int?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableInt();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public long ReadLong(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(long);
        var value = BitConverter.ToInt64(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public long? ReadNullableLong(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        long value = ReadLong();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(long) + sizeof(bool);
        return value;
    }

    public long[] ReadLongs(bool moveIndexPosition = true)
    {
        int length = ReadInt();
        int typeSize = length * sizeof(long) + sizeof(int);
        var value = new long[length];
        for (int i = 0; i < length; i++)
            value[i] = ReadLong();
        CurrentIndex -= moveIndexPosition ? 0 : typeSize;
        return value;
    }

    public long?[] ReadNullableLongs(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new long?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableLong();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public short ReadShort(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(short);
        var value = BitConverter.ToInt16(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public short? ReadNullableShort(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        short value = ReadShort();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(short) + sizeof(bool);
        return value;
    }

    public short[] ReadShorts(bool moveIndexPosition = true)
    {
        int length = ReadInt();
        int typeSize = length * sizeof(short) + sizeof(int);
        var value = new short[length];
        for (int i = 0; i < length; i++)
            value[i] = ReadShort();
        CurrentIndex -= moveIndexPosition ? 0 : typeSize;
        return value;
    }

    public short?[] ReadNullableShorts(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new short?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableShort();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public uint ReadUInt(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(uint);
        var value = BitConverter.ToUInt32(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public uint? ReadNullableUInt(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        uint value = ReadUInt();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(uint) + sizeof(bool);
        return value;
    }

    public uint[] ReadUInts(bool moveIndexPosition = true)
    {
        int length = ReadInt();
        int typeSize = length * sizeof(uint) + sizeof(int);
        var value = new uint[length];
        for (int i = 0; i < length; i++)
            value[i] = ReadUInt();
        CurrentIndex -= moveIndexPosition ? 0 : typeSize;
        return value;
    }

    public uint?[] ReadNullableUInts(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new uint?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableUInt();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public ulong ReadULong(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(ulong);
        var value = BitConverter.ToUInt64(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public ulong? ReadNullableULong(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        ulong value = ReadULong();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(ulong) + sizeof(bool);
        return value;
    }

    public ulong[] ReadULongs(bool moveIndexPosition = true)
    {
        int length = ReadInt();
        int typeSize = length * sizeof(ulong) + sizeof(int);
        var value = new ulong[length];
        for (int i = 0; i < length; i++)
            value[i] = ReadULong();
        CurrentIndex -= moveIndexPosition ? 0 : typeSize;
        return value;
    }

    public ulong?[] ReadNullableULongs(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new ulong?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableULong();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public ushort ReadUShort(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(short);
        var value = BitConverter.ToUInt16(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public ushort? ReadNullableUShort(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        ushort value = ReadUShort();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(ushort) + sizeof(bool);
        return value;
    }

    public ushort[] ReadUShorts(bool moveIndexPosition = true)
    {
        int length = ReadInt();
        int typeSize = length * sizeof(ushort) + sizeof(int);
        var value = new ushort[length];
        for (int i = 0; i < length; i++)
            value[i] = ReadUShort();
        CurrentIndex -= moveIndexPosition ? 0 : typeSize;
        return value;
    }

    public ushort?[] ReadNullableUShorts(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new ushort?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableUShort();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public string ReadString(bool moveIndexPosition = true)
    {
        int strLen = ReadInt(false);
        if (strLen == -1)
        {
            CurrentIndex += moveIndexPosition ? sizeof(int) : 0;
            return null;
        }
        var value = Encoding.ASCII.GetString(byteList.GetRange(CurrentIndex + 4, strLen).ToArray());
        CurrentIndex += moveIndexPosition ? strLen + 4 : 0;
        return value;
    }

    public string[] ReadStrings(bool moveIndexPosition = true)
    {
        int length = ReadInt();
        int typeSize = sizeof(int);
        var value = new string[length];
        for (int i = 0; i < length; i++)
            value[i] = ReadString();
        CurrentIndex -= moveIndexPosition ? 0 : typeSize;
        return value;
    }

    // Command
    public T ReadEnum<T>(bool moveIndexPosition = true) where T : Enum
    {
        ushort enumValue = ReadUShort(moveIndexPosition);
        return (T)Enum.ToObject(typeof(T), enumValue);
    }

    // Unity Structs
    public Vector2 ReadVector2(bool moveIndexPosition = true)
    {
        float x = ReadFloat();
        float y = ReadFloat();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(float) * 2;
        return new Vector2(x, y);
    }

    public Vector2? ReadNullableVector2(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        Vector2 value = ReadVector2();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(float) * 2 + sizeof(bool);
        return value;
    }

    public Vector2[] ReadVector2s(bool moveIndexPosition = true)
    {
        int length = ReadInt();
        int typeSize = length * sizeof(float) * 2 + sizeof(int);
        var value = new Vector2[length];
        for (int i = 0; i < length; i++)
            value[i] = ReadVector2();
        CurrentIndex -= moveIndexPosition ? 0 : typeSize;
        return value;
    }

    public Vector2?[] ReadNullableVector2s(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new Vector2?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableVector2();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public Vector3 ReadVector3(bool moveIndexPosition = true)
    {
        float x = ReadFloat();
        float y = ReadFloat();
        float z = ReadFloat();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(float) * 3;
        return new Vector3(x, y, z);
    }

    public Vector3? ReadNullableVector3(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        Vector3 value = ReadVector3();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(float) * 3 + sizeof(bool);
        return value;
    }

    public Vector3[] ReadVector3s(bool moveIndexPosition = true)
    {
        int length = ReadInt();
        int typeSize = length * sizeof(float) * 3 + sizeof(int);
        var value = new Vector3[length];
        for (int i = 0; i < length; i++)
            value[i] = ReadVector3();
        CurrentIndex -= moveIndexPosition ? 0 : typeSize;
        return value;
    }

    public Vector3?[] ReadNullableVector3s(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new Vector3?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableVector3();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public Quaternion ReadQuaternion(bool moveIndexPosition = true)
    {
        float x = ReadFloat();
        float y = ReadFloat();
        float z = ReadFloat();
        float w = ReadFloat();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(float) * 4;
        return new Quaternion(x, y, z, w);
    }

    public Quaternion? ReadNullableQuaternion(bool moveIndexPosition = true)
    {
        bool hasValue = ReadBool();
        if (!hasValue)
        {
            CurrentIndex -= moveIndexPosition ? 0 : sizeof(bool);
            return null;
        }

        Quaternion value = ReadQuaternion();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(float) * 4 + sizeof(bool);
        return value;
    }

    public Quaternion[] ReadQuaternions(bool moveIndexPosition = true)
    {
        int length = ReadInt();
        int typeSize = length * sizeof(float) * 4 + sizeof(int);
        var value = new Quaternion[length];
        for (int i = 0; i < length; i++)
            value[i] = ReadQuaternion();
        CurrentIndex -= moveIndexPosition ? 0 : typeSize;
        return value;
    }

    public Quaternion?[] ReadNullableQuaternions(bool moveIndexPosition = true)
    {
        int cachedIdx = CurrentIndex;
        int length = ReadInt();
        var value = new Quaternion?[length];
        for (int i = 0; i < length; i++)
        {
            var v = ReadNullableQuaternion();
            value[i] = v;
        }
        CurrentIndex = moveIndexPosition ? CurrentIndex : cachedIdx;
        return value;
    }

    public object ReadObject(Type parameterType, bool moveIndexPosition = true)
    {
        if (parameterType == null)
        {
            throw new ArgumentNullException(nameof(parameterType));
        }

        int startIndex = CurrentIndex;

        try
        {
            Type nullableInner = Nullable.GetUnderlyingType(parameterType);
            // Nullable<T>
            if (nullableInner != null)
            {
                bool hasValue = ReadBool();
                if (!hasValue)
                {
                    return null;
                }

                return ReadObject(nullableInner, true);
            }

            // Array
            if (parameterType.IsArray)
            {
                int length = ReadInt();
                if (length < 0)
                {
                    return null;
                }

                Type elementType = parameterType.GetElementType();
                Type elementNullableInner = Nullable.GetUnderlyingType(elementType);
                Type effectiveElementType = elementNullableInner ?? elementType;

                Array array = Array.CreateInstance(elementType, length);

                // INetSerializable array
                if (typeof(INetSerializable).IsAssignableFrom(effectiveElementType))
                {
                    MethodInfo deserializeMethod = effectiveElementType.GetMethod("Deserialize", BindingFlags.Instance | BindingFlags.Public) ?? throw new Exception($"{effectiveElementType.Name} must implement Deserialize(NetPacket).");

                    if (effectiveElementType.GetConstructor(Type.EmptyTypes) == null)
                    {
                        throw new Exception($"{effectiveElementType.Name} must have a parameterless constructor.");
                    }

                    for (int i = 0; i < length; i++)
                    {
                        bool hasElement = ReadBool();
                        if (!hasElement)
                        {
                            array.SetValue(null, i);
                            continue;
                        }

                        object instance = Activator.CreateInstance(effectiveElementType);
                        object value = deserializeMethod.Invoke(instance, new object[] { this });
                        array.SetValue(value, i);
                    }

                    return array;
                }

                // Primitive array
                for (int i = 0; i < length; i++)
                {
                    array.SetValue(ReadObject(elementType, true), i);
                }

                return array;
            }

            // INetSerializable
            if (typeof(INetSerializable).IsAssignableFrom(parameterType))
            {
                bool hasValue = ReadBool();
                if (!hasValue)
                {
                    return null;
                }

                MethodInfo deserializeMethod = parameterType.GetMethod("Deserialize", BindingFlags.Instance | BindingFlags.Public) ?? throw new Exception($"{parameterType.Name} must implement Deserialize(NetPacket).");

                if (parameterType.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new Exception($"{parameterType.Name} must have a parameterless constructor.");
                }

                object instance = Activator.CreateInstance(parameterType);
                return deserializeMethod.Invoke(instance, new object[] { this });
            }

            // Primitives
            if (parameterType == typeof(byte)) return ReadByte();
            if (parameterType == typeof(byte[])) return ReadBytes();
            if (parameterType == typeof(sbyte)) return ReadSByte();
            if (parameterType == typeof(sbyte[])) return ReadSBytes();
            if (parameterType == typeof(bool)) return ReadBool();
            if (parameterType == typeof(bool[])) return ReadBools();
            if (parameterType == typeof(char)) return ReadChar();
            if (parameterType == typeof(char[])) return ReadChars();
            if (parameterType == typeof(double)) return ReadDouble();
            if (parameterType == typeof(double[])) return ReadDoubles();
            if (parameterType == typeof(float)) return ReadFloat();
            if (parameterType == typeof(float[])) return ReadFloats();
            if (parameterType == typeof(int)) return ReadInt();
            if (parameterType == typeof(int[])) return ReadInts();
            if (parameterType == typeof(long)) return ReadLong();
            if (parameterType == typeof(long[])) return ReadLongs();
            if (parameterType == typeof(short)) return ReadShort();
            if (parameterType == typeof(short[])) return ReadShorts();
            if (parameterType == typeof(uint)) return ReadUInt();
            if (parameterType == typeof(uint[])) return ReadUInts();
            if (parameterType == typeof(ulong)) return ReadULong();
            if (parameterType == typeof(ulong[])) return ReadULongs();
            if (parameterType == typeof(ushort)) return ReadUShort();
            if (parameterType == typeof(ushort[])) return ReadUShorts();
            if (parameterType == typeof(string)) return ReadString();
            if (parameterType == typeof(string[])) return ReadStrings();
            if (parameterType == typeof(Vector2)) return ReadVector2();
            if (parameterType == typeof(Vector2[])) return ReadVector2s();
            if (parameterType == typeof(Vector3)) return ReadVector3();
            if (parameterType == typeof(Vector3[])) return ReadVector3s();
            if (parameterType == typeof(Quaternion)) return ReadQuaternion();
            if (parameterType == typeof(Quaternion[])) return ReadQuaternions();

            throw new Exception($"Unsupported parameter type: {parameterType.FullName}");
        }
        finally
        {
            if (!moveIndexPosition)
            {
                CurrentIndex = startIndex;
            }
        }
    }
}
