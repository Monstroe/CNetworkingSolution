using UnityEngine;

public class MapServerService : ServerService
{
    public Map Map { get; private set; }

    [SerializeField] private Map mapPrefab;

    private bool startingObjectsInitialized = false;

    public override void Init(ServerLobby lobby)
    {
        base.Init(lobby);
        // Init Map
        Map = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity).GetComponent<Map>();
        Map.transform.SetParent(this.transform);
        foreach (Renderer r in Map.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = false;
        }
        foreach (ClientObject obj in Map.GetComponentsInChildren<ClientObject>(true))
        {
            obj.gameObject.TryGetComponent(out Collider objCollider);
            if (objCollider != null)
            {
                objCollider.enabled = false;
            }
            obj.enabled = false;
        }
    }

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
        if (!startingObjectsInitialized)
        {
            foreach (ClientObject clientObj in Map.GetStartingClientObjects())
            {
                lobby.GetService<ObjectServerService>().SpawnObject(joinedUser, clientObj.PrefabKey, clientObj.transform.position, clientObj.transform.rotation, null, true, false);
            }

            startingObjectsInitialized = true;
        }
    }

    public override void UserLeft(UserData leftUser)
    {
        // Nothing
    }
}
