public interface INetSerializable<T>
{
    public void Serialize(ref NetPacket packet);
    public T Deserialize(ref NetPacket packet);
}
