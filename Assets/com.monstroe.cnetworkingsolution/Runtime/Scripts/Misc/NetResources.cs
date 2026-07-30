using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Monstroe.CNetworkingSolution
{
    public class NetResources : MonoBehaviour
    {
        public static NetResources Instance { get; private set; }

        [Header("Transport Settings")]
        [SerializeField] private List<NetTransport> transportPrefabs;

        [Header("Asset Registries")]
        [SerializeField] private string clientPrefabsLabel = "CNS_ClientPrefabs";
        [SerializeField] private string serverPrefabsLabel = "CNS_ServerPrefabs";
        [SerializeField] private string sfxLabel = "CNS_SFX";
        [SerializeField] private string vfxLabel = "CNS_VFX";

        private readonly Dictionary<Type, NetTransport> transportPrefabsDict = new Dictionary<Type, NetTransport>();

        private readonly Dictionary<string, ulong> clientPrefabsPathToKeyMap = new Dictionary<string, ulong>();
        private readonly Dictionary<ulong, string> clientPrefabsKeyToPathMap = new Dictionary<ulong, string>();

        private readonly Dictionary<string, ulong> serverPrefabsPathToKeyMap = new Dictionary<string, ulong>();
        private readonly Dictionary<ulong, string> serverPrefabsKeyToPathMap = new Dictionary<ulong, string>();

        private readonly Dictionary<ulong, ulong> clientToServerPrefabKeyMap = new Dictionary<ulong, ulong>();
        private readonly Dictionary<ulong, ulong> serverToClientPrefabKeyMap = new Dictionary<ulong, ulong>();

        private readonly Dictionary<string, ulong> sfxPathToKeyMap = new Dictionary<string, ulong>();
        private readonly Dictionary<ulong, string> sfxKeyToPathMap = new Dictionary<ulong, string>();

        private readonly Dictionary<string, ulong> vfxPathToKeyMap = new Dictionary<string, ulong>();
        private readonly Dictionary<ulong, string> vfxKeyToPathMap = new Dictionary<ulong, string>();

        async void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Multiple instances of NetResources detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            InitTransports();

            await InitAssetRegistry(clientPrefabsLabel, clientPrefabsPathToKeyMap, clientPrefabsKeyToPathMap);
            await InitAssetRegistry(serverPrefabsLabel, serverPrefabsPathToKeyMap, serverPrefabsKeyToPathMap);
            await InitAssetRegistry(sfxLabel, sfxPathToKeyMap, sfxKeyToPathMap);
            await InitAssetRegistry(vfxLabel, vfxPathToKeyMap, vfxKeyToPathMap);

            foreach ((string path, ulong key) in clientPrefabsPathToKeyMap)
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

        public T GetTransportPrefab<T>() where T : NetTransport
        {
            if (transportPrefabsDict.TryGetValue(typeof(T), out NetTransport transport))
            {
                return (T)transport;
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Transport prefab of type {typeof(T).Name} not found.");
                return null;
            }
        }

        public Tuple<ulong, string> GetServerPrefabFromClientKey(ulong clientKey)
        {
            if (clientToServerPrefabKeyMap.TryGetValue(clientKey, out ulong serverKey))
            {
                return new Tuple<ulong, string>(serverKey, serverPrefabsKeyToPathMap[serverKey]);
            }
            Debug.LogError("NetResources could not find server prefab key for client prefab key '" + clientKey + "'");
            return null;
        }

        public Tuple<ulong, string> GetClientPrefabFromServerKey(ulong serverKey)
        {
            if (serverToClientPrefabKeyMap.TryGetValue(serverKey, out ulong clientKey))
            {
                return new Tuple<ulong, string>(clientKey, clientPrefabsKeyToPathMap[clientKey]);
            }
            Debug.LogError("NetResources could not find client prefab key for server prefab key '" + serverKey + "'");
            return null;
        }

        public ulong GetClientPrefabKeyFromPath(string path)
        {
            return GetKeyFromPath(clientPrefabsPathToKeyMap, path);
        }

        public string GetClientPrefabPathFromKey(ulong key)
        {
            return GetPathFromKey(clientPrefabsKeyToPathMap, key);
        }

        public ulong GetServerPrefabKeyFromPath(string path)
        {
            return GetKeyFromPath(serverPrefabsPathToKeyMap, path);
        }

        public string GetServerPrefabPathFromKey(ulong key)
        {
            return GetPathFromKey(serverPrefabsKeyToPathMap, key);
        }

        public ulong GetSFXKeyFromPath(string path)
        {
            return GetKeyFromPath(sfxPathToKeyMap, path);
        }

        public string GetSFXPathFromKey(ulong key)
        {
            return GetPathFromKey(sfxKeyToPathMap, key);
        }

        public ulong GetVFXKeyFromPath(string path)
        {
            return GetKeyFromPath(vfxPathToKeyMap, path);
        }

        public string GetVFXPathFromKey(ulong key)
        {
            return GetPathFromKey(vfxKeyToPathMap, key);
        }

        private ulong GetKeyFromPath(Dictionary<string, ulong> dict, string path)
        {
            if (dict.TryGetValue(path, out ulong key))
            {
                return key;
            }
            Debug.LogError("NetResources could not find key for object with path '" + path + "'");
            return 0;
        }

        private string GetPathFromKey(Dictionary<ulong, string> dict, ulong key)
        {
            if (dict.TryGetValue(key, out string path))
            {
                return path;
            }
            Debug.LogError("NetResources could not find path for object with key '" + key + "'");
            return null;
        }

        private async Task InitAssetRegistry(string label, Dictionary<string, ulong> nameToIdDict, Dictionary<ulong, string> idToNameDict)
        {
            IList<IResourceLocation> locations = await LoadLocations(label);
            foreach (var location in locations)
            {
                string name = location.PrimaryKey;
                if (nameToIdDict.ContainsKey(name))
                    continue;

                ulong id = GenerateHashKey(name);
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
                transportPrefabsDict.Add(transport.GetType(), transport);
            }
        }

        public static ulong GenerateHashKey(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return BitConverter.ToUInt64(hash, 0);
            }
        }
    }
}
