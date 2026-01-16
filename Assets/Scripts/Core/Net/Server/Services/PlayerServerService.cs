using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public class PlayerServerService : ServerService
{
    public Dictionary<UserData, ServerPlayer> ServerPlayers { get; private set; } = new Dictionary<UserData, ServerPlayer>();

    [SerializeField] private ServerPlayer serverPlayerPrefab;

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
        // Spawning happens first in the Server Service
        foreach (ServerPlayer p in ServerPlayers.Values)
        {
            lobby.SendToUser(joinedUser, PacketBuilder.PlayerSpawn(p.User, p.transform.position, p.transform.rotation, p.IsWalking, p.IsSprinting, p.IsCrouching, p.IsGrounded, p.Jumped, p.Grabbed), TransportMethod.Reliable);
        }

        // Spawn new player
        Map map = lobby.GetService<MapServerService>().Map;
        Transform spawnPoint = map.GetRandomSpawnPoint(ServerPlayers.Values.Select(p => p.transform.position).ToList());
        Vector3 position = map.GetGroundPosition(spawnPoint.position);
        Quaternion rotation = spawnPoint.rotation;

        ServerPlayer player = (ServerPlayer)InstantiateOnServer(serverPlayerPrefab.gameObject, position, rotation, false);
        player.Owner = player; // For server-side movement authority, this should be null
        player.Init(joinedUser.PlayerId, lobby, joinedUser);

        lobby.SendToGame(PacketBuilder.PlayerSpawn(joinedUser, position, rotation, player.IsWalking, player.IsSprinting, player.IsCrouching, player.IsGrounded, player.Jumped, player.Grabbed), TransportMethod.Reliable);
    }

    public override void UserLeft(UserData leftUser)
    {
        if (ServerPlayers.TryGetValue(leftUser, out ServerPlayer player))
        {
            player.Remove();
            Destroy(player.gameObject);
            lobby.SendToGame(PacketBuilder.PlayerDestroy(leftUser), TransportMethod.Reliable);
        }
    }
}
