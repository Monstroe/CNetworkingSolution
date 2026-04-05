using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

[ServiceId("PlayerService")]
public class PlayerServerService : ServerService
{
    public Dictionary<UserData, ServerPlayer> ServerPlayers { get; private set; } = new Dictionary<UserData, ServerPlayer>();

    [SerializeField] private ServerPlayer serverPlayerPrefab;
    [Space]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minDistanceFromPlayers = 5f;

    public override void UserJoinedGame(UserData joinedUser)
    {
        base.UserJoinedGame(joinedUser);
        // Spawn new player
        Transform spawnPoint = GetRandomSpawnPoint();
        Vector3 position = GetGroundPosition(spawnPoint.position);
        Quaternion rotation = spawnPoint.rotation;
        InstantiateOnServer(serverPlayerPrefab.gameObject, position, rotation, joinedUser.PlayerId);
    }

    public override void UserLeft(UserData leftUser)
    {
        base.UserLeft(leftUser);
        if (ServerPlayers.TryGetValue(leftUser, out ServerPlayer player))
        {
            DestroyOnServer(player);
        }
    }

    private Transform GetRandomSpawnPoint()
    {
        List<Vector3> playerPositions = ServerPlayers.Values.Select(p => p.transform.position).ToList();
        Transform spawnPoint;
        do
        {
            spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        } while (playerPositions.Exists(pos => Vector3.Distance(pos, spawnPoint.position) < minDistanceFromPlayers));
        return spawnPoint;
    }

    private Vector3 GetGroundPosition(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 100, Vector3.down, out hit, 200f))
        {
            return hit.point;
        }
        return position;
    }
}
