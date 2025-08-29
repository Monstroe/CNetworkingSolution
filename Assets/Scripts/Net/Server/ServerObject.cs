public abstract class ServerObject : INetObject
{
    public ushort Id { get; protected set; }

    public ServerObject(ushort id)
    {
        this.Id = id;
    }

    public abstract void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod);
    public abstract void Tick(ServerLobby lobby);
}