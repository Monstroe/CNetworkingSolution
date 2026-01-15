using System.Collections;
using System.Net;
using UnityEngine;

public class GameServerService : ServerService
{
    [Tooltip("Delay in seconds before the game starts after the host initiates the start. This allows players time to join the game.")]
    [SerializeField] private float gameStartDelay = 15f;

    private bool gameStarted = false;

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
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

                    lobby.UserJoinedGame(user);
                    break;
                }
            case CommandType.GAME_START:
                {
                    if (gameStarted)
                    {
                        Debug.LogWarning($"User {user.UserId} tried to start the game, but the game has already started.");
                        return;
                    }

                    if (!user.IsHost(lobby.LobbyData))
                    {
                        Debug.LogWarning($"User {user.UserId} tried to start the game, but only the host can start the game.");
                        return;
                    }

                    StartCoroutine(GameLoop());
                    StartCoroutine(GameStartTimer());
                    lobby.SendToLobby(PacketBuilder.GameStart(), TransportMethod.Reliable);
                    break;
                }
        }
    }

    private IEnumerator GameLoop()
    {
        yield return new WaitUntil(() => lobby.LobbyData.GameUsers.Count == lobby.LobbyData.LobbyUsers.Count || gameStarted);

        foreach (var user in lobby.LobbyData.LobbyUsers)
        {
            if (!user.InGame)
            {
                Debug.LogWarning($"User {user.UserId} did not join the game in time. Kicking from lobby.");
                lobby.KickUser(user);
            }
        }

        // Game logic here

    }

    private IEnumerator GameStartTimer()
    {
        yield return new WaitForSeconds(gameStartDelay);
        gameStarted = true;
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
        joinedUser.InGame = true;
        lobby.SendToLobby(PacketBuilder.GameUserJoined(joinedUser), TransportMethod.Reliable);
    }

    public override void UserLeft(UserData leftUser)
    {
        // Nothing
    }
}
