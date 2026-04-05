using System.Threading.Tasks;
using UnityEngine;

[ServiceId("GameService")]
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

    public override async void ReceiveData(UserData user, NetPacket packet, ushort commandType, TransportMethod? transportMethod)
    {
        base.ReceiveData(user, packet, commandType, transportMethod);
        switch ((GameCommandType)commandType)
        {
            case GameCommandType.GAME_USER_JOINED:
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
                    GameUserJoinedEvent evt = new GameUserJoinedEvent()
                    {
                        User = user
                    };
                    var result = await lobby.TriggerGameEvent(evt);
                    if (!result.Canceled)
                    {
                        lobby.UserJoinedGame(user);
                    }
                    break;
                }
        }
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        base.UserJoinedGame(joinedUser);
        SendToLobbyClientServices(GamePacketBuilder.GameUserJoined(joinedUser), TransportMethod.Reliable);
    }
}

public class GameUserJoinedEvent : GameEvent
{
    public UserData User { get; set; }
}
