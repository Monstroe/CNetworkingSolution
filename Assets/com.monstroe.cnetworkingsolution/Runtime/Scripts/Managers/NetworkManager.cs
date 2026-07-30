using UnityEngine;

namespace Monstroe.CNetworkingSolution
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        public ClientManager Client => clientInstance;
        public ServerManager Server => serverInstance;

        [Header("Network Prefabs")]
        [SerializeField] private ClientManager clientPrefab;
        [SerializeField] private ServerManager serverPrefab;

        [Header("Network Instances (if needed)")]
        [SerializeField] private ClientManager clientInstance;
        [SerializeField] private ServerManager serverInstance;

        [Space]
        [SerializeField] private bool autoSpawnClient = true;
        [SerializeField] private bool autoSpawnServer = false;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Multiple instances of NetworkManager detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            if (autoSpawnClient)
            {
                SpawnClient();
            }

            if (autoSpawnServer)
            {
                SpawnServer();
            }
        }

        public ClientManager SpawnClient()
        {
            if (clientInstance == null)
            {
                clientInstance = Instantiate(clientPrefab, this.transform);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientManager instance already exists.");
            }
            return clientInstance;
        }

        public bool DestroyClient()
        {
            if (clientInstance != null)
            {
                Destroy(clientInstance.gameObject);
                clientInstance = null;
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No ClientManager instance to destroy.");
            }
            return clientInstance == null;
        }

        public ServerManager SpawnServer()
        {
            if (serverInstance == null)
            {
                serverInstance = Instantiate(serverPrefab, this.transform);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerManager instance already exists.");
            }
            return serverInstance;
        }

        public bool DestroyServer()
        {
            if (serverInstance != null)
            {
                Destroy(serverInstance.gameObject);
                serverInstance = null;
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No ServerManager instance to destroy.");
            }
            return serverInstance == null;
        }
    }
}
