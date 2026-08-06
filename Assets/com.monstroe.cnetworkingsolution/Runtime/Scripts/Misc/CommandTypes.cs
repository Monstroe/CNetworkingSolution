namespace Monstroe.CNetworkingSolution
{
    internal enum ConnectionCommandType
    {
        CONNECTION_REQUEST = 0,
        CONNECTION_RESPONSE = 1,
    }

    internal enum ReservedCommandType
    {
        RPC = ushort.MaxValue
    }

    internal enum ObjectCommandType
    {
        OBJECT_SPAWN_REQUEST = ushort.MaxValue - 1,
        OBJECT_DESTROY_REQUEST = ushort.MaxValue - 2,
        OBJECT_COMMUNICATION = ushort.MaxValue - 3,
        OBJECT_SPAWN = ushort.MaxValue - 4,
        OBJECT_DESTROY = ushort.MaxValue - 5,
        OBJECT_TRANSFORM = ushort.MaxValue - 6,
        OBJECTS_INIT = ushort.MaxValue - 7
    }
}