namespace Monstroe.CNetworkingSolution
{
    public interface INetSerializable
    {
        public void Serialize(NetPacket packet);
    }

    public interface INetSerializable<T> : INetSerializable
    {
        public T Deserialize(NetPacket packet);
    }
}
