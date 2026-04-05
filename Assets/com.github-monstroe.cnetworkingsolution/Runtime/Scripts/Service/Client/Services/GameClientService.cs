using System.Linq;
using UnityEngine;

[ServiceId("GameService")]
public class GameClientService : ClientService
{
    public delegate void GameStartedEventHandler();
    public event GameStartedEventHandler OnGameStarted;

    public delegate void GameInitializedEventHandler();
    public event GameInitializedEventHandler OnGameInitialized;

    public delegate void GameUserJoinedEventHandler(UserData user);
    public event GameUserJoinedEventHandler OnGameUserJoined;

    public override void ReceiveData(NetPacket packet, ushort commandType, TransportMethod? transportMethod)
    {
        base.ReceiveData(packet, commandType, transportMethod);
        switch ((GameCommandType)commandType)
        {
            case GameCommandType.GAME_USER_JOINED:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = lobby.LobbyData.LobbyUsers.FirstOrDefault(u => u.PlayerId == playerId);
                    if (user == null)
                    {
                        Debug.LogWarning($"Received GAME_USER_JOINED for player ID {playerId}, but no such user was found in the lobby.");
                        return;
                    }

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
        }
    }
}
