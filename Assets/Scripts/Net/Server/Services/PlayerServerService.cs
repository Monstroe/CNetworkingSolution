using System.Linq;
using UnityEngine;

public class PlayerServerService : ServerService
{
    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void Tick(ServerLobby lobby)
    {
        // Nothing
    }

    public override void UserJoined(ServerLobby lobby, UserData joinedUser)
    {
        // Nothing
    }

    public override void UserJoinedGame(ServerLobby lobby, UserData joinedUser)
    {
        // Spawning happens first in the Server Service
        foreach (ServerPlayer p in lobby.GameData.ServerPlayers.Values)
        {
            lobby.SendToUser(joinedUser, PacketBuilder.PlayerSpawn(p.User), TransportMethod.Reliable);
        }

        // Spawn new player
        Transform spawnPoint = lobby.Map.GetRandomSpawnPoint(lobby.GameData.ServerPlayers.Values.Select(p => p.Position).ToList());
        Vector3 position = lobby.Map.GetGroundPosition(spawnPoint.position);
        Quaternion rotation = spawnPoint.rotation;
        Vector3 forward = spawnPoint.forward;

        ServerPlayer player = new ServerPlayer(joinedUser.PlayerId, joinedUser);
        player.Position = position;
        player.Rotation = rotation;
        player.Forward = forward;
        lobby.GameData.ServerPlayers.Add(joinedUser, player);
        lobby.GameData.ServerObjects.Add(player.Id, player);

        lobby.SendToGame(PacketBuilder.PlayerSpawn(joinedUser), TransportMethod.Reliable);
        lobby.SendToGame(PacketBuilder.ObjectCommunication(player, PacketBuilder.PlayerTransform(position, rotation, forward)), TransportMethod.Reliable);
    }

    public override void UserLeft(ServerLobby lobby, UserData leftUser)
    {
        if (lobby.GameData.ServerPlayers.TryGetValue(leftUser, out ServerPlayer player))
        {
            lobby.GameData.ServerPlayers.Remove(leftUser);
            lobby.GameData.ServerObjects.Remove(player.Id);

            if (player.CurrentInteractable != null)
            {
                player.CurrentInteractable.Drop(player, lobby, leftUser, null, TransportMethod.Reliable);
                lobby.SendToGame(PacketBuilder.ObjectCommunication(player.CurrentInteractable, PacketBuilder.PlayerDrop(player.User.PlayerId)), TransportMethod.Reliable);
            }
            lobby.SendToGame(PacketBuilder.PlayerDestroy(leftUser), TransportMethod.Reliable);
        }
    }
}
