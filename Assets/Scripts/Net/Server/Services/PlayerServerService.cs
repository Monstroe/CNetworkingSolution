using System.Linq;
using UnityEngine;

public class PlayerServerService : ServerService
{
    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.PLAYER_SPAWN:
                {
                    if (!lobby.GameData.ServerPlayers.ContainsKey(user))
                    {
                        Transform spawnPoint = lobby.Map.GetRandomSpawnPoint(lobby.GameData.ServerPlayers.Values.Select(p => p.Position).ToList());
                        Vector3 position = lobby.Map.GetGroundPosition(spawnPoint.position);
                        Quaternion rotation = spawnPoint.rotation;
                        Vector3 forward = spawnPoint.forward;

                        ServerPlayer player = new ServerPlayer(user.PlayerId, user);
                        player.Position = position;
                        player.Rotation = rotation;
                        player.Forward = forward;
                        lobby.GameData.ServerPlayers.Add(user, player);
                        lobby.GameData.ServerObjects.Add(player.Id, player);
                        lobby.SendToLobby(PacketBuilder.PlayerSpawn(user, position, rotation, forward), transportMethod ?? TransportMethod.Reliable);
                    }
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
        // Send players list
        lobby.SendToUser(user, PacketBuilder.PlayersList(lobby.GameData.ServerPlayers.Values.Where(p => p.Id != user.PlayerId).ToList()), TransportMethod.Reliable);

        // Spawn player
        Transform spawnPoint = lobby.Map.GetRandomSpawnPoint(lobby.GameData.ServerPlayers.Values.Select(p => p.Position).ToList());
        Vector3 position = lobby.Map.GetGroundPosition(spawnPoint.position);
        Quaternion rotation = spawnPoint.rotation;
        Vector3 forward = spawnPoint.forward;

        ServerPlayer player = new ServerPlayer(user.PlayerId, user);
        player.Position = position;
        player.Rotation = rotation;
        player.Forward = forward;
        lobby.GameData.ServerPlayers.Add(user, player);
        lobby.GameData.ServerObjects.Add(player.Id, player);
        lobby.SendToLobby(PacketBuilder.PlayerSpawn(user, position, rotation, forward), TransportMethod.Reliable);
    }

    public override void UserLeft(ServerLobby lobby, UserData user)
    {
        if (lobby.GameData.ServerPlayers.TryGetValue(user, out ServerPlayer player))
        {
            lobby.GameData.ServerPlayers.Remove(user);
            lobby.GameData.ServerObjects.Remove(player.Id);
            lobby.SendToLobby(PacketBuilder.ObjectCommunication(player, PacketBuilder.PlayerDestroy()), TransportMethod.Reliable, user);
        }
    }
}
