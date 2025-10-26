using System.Linq;
using UnityEngine;

public class PlayerServerService : ServerService
{
    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
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
        // Spawning happens first in the Server Service
        foreach (ServerPlayer p in lobby.GameData.ServerPlayers.Values)
        {
            lobby.SendToUser(joinedUser, PacketBuilder.PlayerSpawn(p.User), TransportMethod.Reliable);
        }

        // Spawn new player
        Transform spawnPoint = lobby.Map.GetRandomSpawnPoint(lobby.GameData.ServerPlayers.Values.Select(p => p.transform.position).ToList());
        Vector3 position = lobby.Map.GetGroundPosition(spawnPoint.position);
        Quaternion rotation = spawnPoint.rotation;
        Vector3 forward = spawnPoint.forward;

        ServerPlayer player = (ServerPlayer)InstantiateOnServer(NetResources.Instance.ServerPlayerPrefab.gameObject, position, rotation, false);
        player.Owner = player; // For server-side movement authority, this should be null
        player.Init(joinedUser.PlayerId, lobby, joinedUser, position, rotation, forward);

        lobby.SendToGame(PacketBuilder.PlayerSpawn(joinedUser), TransportMethod.Reliable);
    }

    public override void UserLeft(UserData leftUser)
    {
        if (lobby.GameData.ServerPlayers.TryGetValue(leftUser, out ServerPlayer player))
        {
            player.Remove();
            Destroy(player.gameObject);
            lobby.SendToGame(PacketBuilder.PlayerDestroy(leftUser), TransportMethod.Reliable);
        }
    }
}
