using System.Net;

public class FXClientService : ClientService
{
    public ClientFX FX { get; private set; }

    public void SetFX(ClientFX fx)
    {
        FX = fx;
    }

    public override void ReceiveData(NetPacket packet, ushort commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ushort commandType)
    {
        // Nothing
    }
}
