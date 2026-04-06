internal enum ConnectionCommandType
{
    CONNECTION_REQUEST,
    CONNECTION_RESPONSE,
}

internal enum ReservedCommandType
{
    RPC = ushort.MaxValue
}

internal enum ObjectCommandType
{
    OBJECT_SPAWN_REQUEST,
    OBJECT_DESTROY_REQUEST,
    OBJECT_COMMUNICATION,
    OBJECT_SPAWN,
    OBJECT_DESTROY,
    OBJECT_TRANSFORM,
    OBJECTS_INIT
}
