using System;
using System.Collections.Generic;
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

    public void Write(byte[] value)
    {
        Write(value.Length);
        byteList.AddRange(value);
    }

    public void Write(sbyte value)
    {
        byteList.Add((byte)value);
    }

    public void Write(sbyte[] value)
    {
        Write(value.Length);
        byteList.AddRange(Array.ConvertAll(value, b => (byte)b));
    }

    public void Write(bool value)
    {
        byteList.Add((byte)(value ? 1 : 0));
    }

    public void Write(bool[] value)
    {
        Write(value.Length);
        byteList.AddRange(Array.ConvertAll(value, b => (byte)(b ? 1 : 0)));
    }

    public void Write(char value)
    {
        byteList.Add((byte)value);
    }

    public void Write(char[] value)
    {
        Write(value.Length);
        byteList.AddRange(Array.ConvertAll(value, b => (byte)b));
    }

    public void Write(double value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(double[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(float value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(float[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(int value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(int[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(long value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(long[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(short value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(short[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(uint value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(uint[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(ulong value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(ulong[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(ushort value)
    {
        byteList.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(ushort[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(string value)
    {
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

    // Unity Structs
    public void Write(Vector2 value)
    {
        Write(value.x);
        Write(value.y);
    }

    public void Write(Vector2[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(Vector3 value)
    {
        Write(value.x);
        Write(value.y);
        Write(value.z);
    }

    public void Write(Vector3[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write(Quaternion value)
    {
        Write(value.x);
        Write(value.y);
        Write(value.z);
        Write(value.w);
    }

    public void Write(Quaternion[] value)
    {
        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public byte ReadByte(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(byte);
        var value = byteList[CurrentIndex];
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public byte[] ReadBytes(bool moveIndexPosition = true)
    {
        int length = ReadInt(false);
        var value = byteList.GetRange(CurrentIndex + sizeof(int), length).ToArray();
        CurrentIndex += moveIndexPosition ? length + sizeof(int) : 0;
        return value;
    }

    public sbyte ReadSByte(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(sbyte);
        var value = (sbyte)byteList[CurrentIndex];
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public sbyte[] ReadSBytes(bool moveIndexPosition = true)
    {
        int length = ReadInt(false);
        var value = Array.ConvertAll(byteList.GetRange(CurrentIndex + sizeof(int), length).ToArray(), b => (sbyte)b);
        CurrentIndex += moveIndexPosition ? length + sizeof(int) : 0;
        return value;
    }

    public bool ReadBool(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(bool);
        var value = byteList[CurrentIndex] != 0;
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public bool[] ReadBools(bool moveIndexPosition = true)
    {
        int length = ReadInt(false);
        var value = Array.ConvertAll(byteList.GetRange(CurrentIndex + sizeof(int), length).ToArray(), b => b != 0);
        CurrentIndex += moveIndexPosition ? length + sizeof(int) : 0;
        return value;
    }

    public char ReadChar(bool moveIndexPosition = true)
    {
        int typeSize = 1;
        var value = (char)byteList[CurrentIndex];
        CurrentIndex += moveIndexPosition ? typeSize : 0;
        return value;
    }

    public char[] ReadChars(bool moveIndexPosition = true)
    {
        int length = ReadInt(false);
        var value = Array.ConvertAll(byteList.GetRange(CurrentIndex + sizeof(int), length).ToArray(), b => (char)b);
        CurrentIndex += moveIndexPosition ? length + sizeof(int) : 0;
        return value;
    }

    public double ReadDouble(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(double);
        var value = BitConverter.ToDouble(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
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

    public float ReadFloat(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(float);
        var value = BitConverter.ToSingle(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
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

    public int ReadInt(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(int);
        var value = BitConverter.ToInt32(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
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

    public long ReadLong(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(long);
        var value = BitConverter.ToInt64(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
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

    public short ReadShort(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(short);
        var value = BitConverter.ToInt16(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
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

    public uint ReadUInt(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(uint);
        var value = BitConverter.ToUInt32(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
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

    public ulong ReadULong(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(ulong);
        var value = BitConverter.ToUInt64(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
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

    public ushort ReadUShort(bool moveIndexPosition = true)
    {
        int typeSize = sizeof(short);
        var value = BitConverter.ToUInt16(byteList.GetRange(CurrentIndex, typeSize).ToArray());
        CurrentIndex += moveIndexPosition ? typeSize : 0;
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

    public string ReadString(bool moveIndexPosition = true)
    {
        int strLen = ReadInt(false);
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

    // Unity Structs
    public Vector2 ReadVector2(bool moveIndexPosition = true)
    {
        float x = ReadFloat();
        float y = ReadFloat();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(float) * 2;
        return new Vector2(x, y);
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

    public Vector3 ReadVector3(bool moveIndexPosition = true)
    {
        float x = ReadFloat();
        float y = ReadFloat();
        float z = ReadFloat();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(float) * 3;
        return new Vector3(x, y, z);
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

    public Quaternion ReadQuaternion(bool moveIndexPosition = true)
    {
        float x = ReadFloat();
        float y = ReadFloat();
        float z = ReadFloat();
        float w = ReadFloat();
        CurrentIndex -= moveIndexPosition ? 0 : sizeof(float) * 4;
        return new Quaternion(x, y, z, w);
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
}
