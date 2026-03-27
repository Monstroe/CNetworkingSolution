using System.Net;
using UnityEngine;

public class GameClientService : ClientService
{
    public delegate void GameStartedEventHandler();
    public event GameStartedEventHandler OnGameStarted;

    public delegate void GameInitializedEventHandler();
    public event GameInitializedEventHandler OnGameInitialized;

    public delegate void GameUserJoinedEventHandler(UserData user);
    public event GameUserJoinedEventHandler OnGameUserJoined;

    public override void ReceiveData(NetPacket packet, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.GAME_USER_JOINED:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = lobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    user.InGame = true;

                    if (lobby.CurrentUser.PlayerId == playerId)
                    {
                        OnGameInitialized?.Invoke();
                    }
                    else
                    {
                        OnGameUserJoined?.Invoke(user);
                    }
                    break;
                }
            case CommandType.GAME_START:
                {
                    OnGameStarted?.Invoke();
                    break;
                }
        }
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, CommandType commandType)
    {
        // Nothing
    }

    /* PACKETS */

    public static NetPacket GameStart()
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<GameServerService>());
        packet.Write((byte)GameServerService.GameCommandType.GAME_START);
        return packet;
    }

    public static NetPacket GameUserJoined(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write(NetResources.GenerateServiceId<GameServerService>());
        packet.Write((byte)GameServerService.GameCommandType.GAME_USER_JOINED);
        packet.Write(user.PlayerId);
        return packet;
    }
}
