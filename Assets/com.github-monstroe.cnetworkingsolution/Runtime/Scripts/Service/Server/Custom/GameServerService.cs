using System.Collections;
using System.Net;
using UnityEngine;

public class GameServerService : ServerService
{
    public delegate void GameUserJoinedEventHandler(UserData user);
    public event GameUserJoinedEventHandler OnGameUserJoined;

    // The game service is special because it handles when users join the game and when the game starts
    // It needs to run last (but before the lobby service)
    public override void Init(ServerLobby lobby)
    {
        base.Init(lobby);
    }

    public override void ReceiveData(UserData user, NetPacket packet, ushort commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.GAME_USER_JOINED:
                {
                    if (user.InGame)
                    {
                        Debug.LogWarning($"User {user.UserId} tried to join the game, but they are already marked as in-game.");
                        return;
                    }

                    byte playerId = packet.ReadByte();
                    if (playerId != user.PlayerId)
                    {
                        Debug.LogWarning($"Player {user.PlayerId} tried to set join game for player {playerId}, but each individual player is responsible for joining the game.");
                        return;
                    }

                    user.InGame = true;
                    OnGameUserJoined?.Invoke(user);
                    lobby.UserJoinedGame(user);
                    break;
                }
        }
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ushort commandType)
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
        lobby.SendToLobby(GamePacketBuilder.GameUserJoined(joinedUser), TransportMethod.Reliable);
    }

    public override void UserLeft(UserData leftUser)
    {
        // Nothing
    }
}
