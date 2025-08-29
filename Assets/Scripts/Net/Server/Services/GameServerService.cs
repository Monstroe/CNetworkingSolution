using UnityEngine;

public class GameServerService : ServerService
{
    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.GAME_USER_JOINED:
                {
                    byte playerId = packet.ReadByte();
                    if (playerId != user.PlayerId)
                    {
                        Debug.LogWarning($"Player {user.PlayerId} tried to set join game for player {playerId}, but each individual player is responsible for joining the game.");
                        return;
                    }

                    lobby.UserJoinedGame(user);
                    break;
                }
        }
    }

    public override void Tick(ServerLobby lobby)
    {

    }

    public override void UserJoined(ServerLobby lobby, UserData user)
    {

    }

    public override void UserJoinedGame(ServerLobby lobby, UserData user)
    {

    }

    public override void UserLeft(ServerLobby lobby, UserData user)
    {

    }
}
