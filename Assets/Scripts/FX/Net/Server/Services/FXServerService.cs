using System.Net;
using UnityEngine;

public class FXServerService : ServerService
{
    [SerializeField] private ServerFX fxPrefab;

    public ServerFX FX { get; private set; }

    public override void Init(ServerLobby lobby)
    {
        base.Init(lobby);
        FX = (ServerFX)InstantiateOnServer(fxPrefab.gameObject);
    }

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }

    public override void Tick()
    {
        // Nothing
    }

    public override void UserJoined(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserLeft(UserData leftUser)
    {
        // Nothing
    }
}
