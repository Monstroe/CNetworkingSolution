using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Map : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minDistanceFromPlayers = 5f;

    public Transform GetRandomSpawnPoint(List<Vector3> playerPositions)
    {
        Transform spawnPoint;
        do
        {
            spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        } while (playerPositions.Exists(pos => Vector3.Distance(pos, spawnPoint.position) < minDistanceFromPlayers));
        return spawnPoint;
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

    public List<ClientObject> GetStartingClientObjects()
    {
        return gameObject.GetComponentsInChildren<ClientObject>(true).OrderBy(n => n.transform.GetSiblingIndex()).ToList();
    }
}