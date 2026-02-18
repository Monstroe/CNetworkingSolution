using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

public class NetResources : MonoBehaviour
{
    public static NetResources Instance { get; private set; }

    public ServerManager ServerPrefab => serverPrefab;
    public Dictionary<TransportType, NetTransport> TransportPrefabs { get; private set; } = new Dictionary<TransportType, NetTransport>();

    public string GameSceneName => gameSceneName;
    public string MenuSceneName => menuSceneName;
    public string ServerSceneName => serverSceneName;

    public NetMode DefaultNetMode => defaultNetMode;
    public int DefaultLobbyId => defaultLobbyId;
    public LobbySettings DefaultLobbySettings => defaultLobbySettings;
    public UserSettings DefaultUserSettings => defaultUserSettings;
    public TransportSettings DefaultTransportSettings => defaultTransportSettings;

    [Header("Connection Settings")]
    [SerializeField] private ServerManager serverPrefab;
    [SerializeField] private List<NetTransport> transportPrefabs;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private string serverSceneName = "Server";

    [Header("Asset Registries")]
    [SerializeField] private string clientPrefabsLabel = "CNS_ClientPrefabs";
    [SerializeField] private string serverPrefabsLabel = "CNS_ServerPrefabs";
    [SerializeField] private string sfxLabel = "CNS_SFX";
    [SerializeField] private string vfxLabel = "CNS_VFX";

    [Header("Default Settings")]
    [SerializeField] private NetMode defaultNetMode = NetMode.Online;
    [Space]
    [SerializeField] private int defaultLobbyId = 0;
    [SerializeField] private LobbySettings defaultLobbySettings = new LobbySettings();
    [Space]
    [SerializeField] private UserSettings defaultUserSettings = new UserSettings();
    [Space]
    [SerializeField] private TransportSettings defaultTransportSettings = new TransportSettings();

    private readonly Dictionary<string, int> clientPrefabsPathToKeyMap = new Dictionary<string, int>();
    private readonly Dictionary<int, string> clientPrefabsKeyToPathMap = new Dictionary<int, string>();

    private readonly Dictionary<string, int> serverPrefabsPathToKeyMap = new Dictionary<string, int>();
    private readonly Dictionary<int, string> serverPrefabsKeyToPathMap = new Dictionary<int, string>();

    private readonly Dictionary<int, int> clientToServerPrefabKeyMap = new Dictionary<int, int>();
    private readonly Dictionary<int, int> serverToClientPrefabKeyMap = new Dictionary<int, int>();

    private readonly Dictionary<string, int> sfxPathToKeyMap = new Dictionary<string, int>();
    private readonly Dictionary<int, string> sfxKeyToPathMap = new Dictionary<int, string>();

    private readonly Dictionary<string, int> vfxPathToKeyMap = new Dictionary<string, int>();
    private readonly Dictionary<int, string> vfxKeyToPathMap = new Dictionary<int, string>();

    async void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of NetResources detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        InitTransports();

        await InitAssetRegistry(clientPrefabsLabel, clientPrefabsPathToKeyMap, clientPrefabsKeyToPathMap);
        await InitAssetRegistry(serverPrefabsLabel, serverPrefabsPathToKeyMap, serverPrefabsKeyToPathMap);
        await InitAssetRegistry(sfxLabel, sfxPathToKeyMap, sfxKeyToPathMap);
        await InitAssetRegistry(vfxLabel, vfxPathToKeyMap, vfxKeyToPathMap);

        foreach ((string path, int key) in clientPrefabsPathToKeyMap)
        {
            var handle = await Addressables.LoadAssetAsync<GameObject>(path).Task;
            handle.TryGetComponent(out ClientObject clientObject);
            if (clientObject == null || clientObject.ServerPrefab == null)
            {
                Debug.LogError("NetResources could not find ClientObject or its ServerPrefab component on loaded client prefab with path '" + path + "'");
                continue;
            }
            clientToServerPrefabKeyMap.Add(key, clientObject.ServerPrefab.PrefabKey);
            serverToClientPrefabKeyMap.Add(clientObject.ServerPrefab.PrefabKey, key);
        }
    }

    public Tuple<int, string> GetServerPrefabFromClientKey(int clientKey)
    {
        if (clientToServerPrefabKeyMap.TryGetValue(clientKey, out int serverKey))
        {
            return new Tuple<int, string>(serverKey, serverPrefabsKeyToPathMap[serverKey]);
        }
        Debug.LogError("NetResources could not find server prefab key for client prefab key '" + clientKey + "'");
        return null;
    }

    public Tuple<int, string> GetClientPrefabFromServerKey(int serverKey)
    {
        if (serverToClientPrefabKeyMap.TryGetValue(serverKey, out int clientKey))
        {
            return new Tuple<int, string>(clientKey, clientPrefabsKeyToPathMap[clientKey]);
        }
        Debug.LogError("NetResources could not find client prefab key for server prefab key '" + serverKey + "'");
        return null;
    }

    public int GetClientPrefabKeyFromPath(string path)
    {
        return GetKeyFromPath(clientPrefabsPathToKeyMap, path);
    }

    public string GetClientPrefabPathFromKey(int key)
    {
        return GetPathFromKey(clientPrefabsKeyToPathMap, key);
    }

    public int GetServerPrefabKeyFromPath(string path)
    {
        return GetKeyFromPath(serverPrefabsPathToKeyMap, path);
    }

    public string GetServerPrefabPathFromKey(int key)
    {
        return GetPathFromKey(serverPrefabsKeyToPathMap, key);
    }

    public int GetSFXKeyFromPath(string path)
    {
        return GetKeyFromPath(sfxPathToKeyMap, path);
    }

    public string GetSFXPathFromKey(int key)
    {
        return GetPathFromKey(sfxKeyToPathMap, key);
    }

    public int GetVFXKeyFromPath(string path)
    {
        return GetKeyFromPath(vfxPathToKeyMap, path);
    }

    public string GetVFXPathFromKey(int key)
    {
        return GetPathFromKey(vfxKeyToPathMap, key);
    }

    private int GetKeyFromPath(Dictionary<string, int> dict, string path)
    {
        if (dict.TryGetValue(path, out int key))
        {
            return key;
        }
        Debug.LogError("NetResources could not find key for object with path '" + path + "'");
        return 0;
    }

    private string GetPathFromKey(Dictionary<int, string> dict, int key)
    {
        if (dict.TryGetValue(key, out string path))
        {
            return path;
        }
        Debug.LogError("NetResources could not find path for object with key '" + key + "'");
        return null;
    }

    private async Task InitAssetRegistry(string label, Dictionary<string, int> nameToIdDict, Dictionary<int, string> idToNameDict)
    {
        IList<IResourceLocation> locations = await LoadLocations(label);
        foreach (var location in locations)
        {
            string name = location.PrimaryKey;
            if (nameToIdDict.ContainsKey(name))
                continue;

            int id = HashPathToId(name);
            nameToIdDict.Add(name, id);
            idToNameDict.Add(id, name);
        }
    }

    private async Task<IList<IResourceLocation>> LoadLocations(string label)
    {
        var handle = Addressables.LoadResourceLocationsAsync(label);
        return await handle.Task;
    }

    private void InitTransports()
    {
        foreach (NetTransport transport in transportPrefabs)
        {
            switch (transport)
            {
#if CNS_TRANSPORT_LOCAL
                case LocalTransport:
                    transport.TransportData.TransportType = TransportType.Local;
                    break;
#endif
#if CNS_TRANSPORT_CNET && CNS_TRANSPORT_CNETRELAY && CNS_TRANSPORT_LOCAL && CNS_SYNC_HOST && CNS_LOBBY_MULTIPLE
                case CNetRelayTransport:
                    transport.TransportData.TransportType = TransportType.CNetRelay;
                    break;
#endif
#if CNS_TRANSPORT_CNET && CNS_TRANSPORT_CNETBROADCAST
                case CNetBroadcastTransport:
                    transport.TransportData.TransportType = TransportType.CNetBroadcast;
                    break;
#endif
#if CNS_TRANSPORT_CNET
                case CNetTransport:
                    transport.TransportData.TransportType = TransportType.CNet;
                    break;
#endif
#if CNS_TRANSPORT_LITENETLIB && CNS_TRANSPORT_LITENETLIBRELAY && CNS_TRANSPORT_LOCAL && CNS_SYNC_HOST && CNS_LOBBY_MULTIPLE
                case LiteNetLibRelayTransport:
                    transport.TransportData.TransportType = TransportType.LiteNetLibRelay;
                    break;
#endif
#if CNS_TRANSPORT_LITENETLIB && CNS_TRANSPORT_LITENETLIBBROADCAST
                case LiteNetLibBroadcastTransport:
                    transport.TransportData.TransportType = TransportType.LiteNetLibBroadcast;
                    break;
#endif
#if CNS_TRANSPORT_LITENETLIB
                case LiteNetLibTransport:
                    transport.TransportData.TransportType = TransportType.LiteNetLib;
                    break;
#endif
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
                case SteamRelayTransport:
                    transport.TransportData.TransportType = TransportType.SteamRelay;
                    break;
#endif
            }
            TransportPrefabs.Add(transport.TransportData.TransportType, transport);
        }
    }

    public static int HashPathToId(string path)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(path));
            return BitConverter.ToInt32(hash, 0);
        }
    }
}

public enum NetMode
{
    Local,
    Online,
}
