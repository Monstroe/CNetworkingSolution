using System;

namespace Monstroe.CNetworkingSolution
{
    public interface INetSerializable
    {
        public void Serialize(NetPacket packet);
        public void DeserializeInto(NetPacket packet);
    }

    public interface INetSerializable<T> : INetSerializable
    {
        public static T Deserialize(NetPacket packet) => throw new NotImplementedException();
    }
}
