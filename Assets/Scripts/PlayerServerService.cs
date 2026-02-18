using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public class PlayerServerService : ServerService
{
    public Dictionary<UserData, ServerPlayer> ServerPlayers { get; private set; } = new Dictionary<UserData, ServerPlayer>();

    [SerializeField] private ServerPlayer serverPlayerPrefab;
    [Space]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minDistanceFromPlayers = 5f;

    public override void ReceiveData(UserData user, NetPacket packet, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, CommandType commandType)
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
        /*foreach (ServerPlayer p in ServerPlayers.Values)
        {
            //lobby.GetComponent<ObjectServerService>().SpawnObject(joinedUser, );

            lobby.SendToUser(joinedUser, PacketBuilder.ObjectSpawn(p.User.PlayerId, p.transform.position, p.transform.rotation, p.OwnerId), TransportMethod.Reliable);
        }

        // Spawn new player
        Transform spawnPoint = GetRandomSpawnPoint();
        Vector3 position = GetGroundPosition(spawnPoint.position);
        Quaternion rotation = spawnPoint.rotation;

        ServerPlayer player = (ServerPlayer)InstantiateOnServer(serverPlayerPrefab.gameObject, position, rotation, false);
        player.Owner = player; // For server-side movement authority, this should be null

        lobby.SendToGame(PacketBuilder.PlayerSpawn(joinedUser, position, rotation, player.IsWalking, player.IsSprinting, player.IsCrouching, player.IsGrounded, player.Jumped, player.Grabbed), TransportMethod.Reliable);
        player.Init(joinedUser.PlayerId, lobby, joinedUser);*/
    }

    public override void UserLeft(UserData leftUser)
    {
        if (ServerPlayers.TryGetValue(leftUser, out ServerPlayer player))
        {
            player.Remove();
            //Destroy(player.gameObject);
            lobby.SendToGame(PacketBuilder.PlayerDestroy(leftUser), TransportMethod.Reliable);
        }
    }

    private Transform GetRandomSpawnPoint()
    {
        return null;
        /*List<Vector3> playerPositions = ServerPlayers.Values.Select(p => p.transform.position).ToList();
        Transform spawnPoint;
        do
        {
            spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        } while (playerPositions.Exists(pos => Vector3.Distance(pos, spawnPoint.position) < minDistanceFromPlayers));
        return spawnPoint;*/
    }

    public Vector3 GetGroundPosition(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 100, Vector3.down, out hit, 200f, GameResources.Instance.GroundMask))
        {
            return hit.point;
        }
        return position;
    }
}
