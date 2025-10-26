public interface INetSerializable<T>
{
    public void Serialize(NetPacket packet);
    public T Deserialize(NetPacket packet);
}
