using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance { get; private set; }

    public ulong ServerTick { get; private set; } = 0;
    public ServerData ServerData { get; private set; } = new ServerData();

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
    public delegate void PublicIpAddressFetchedHandler(string ipAddress);
    public event PublicIpAddressFetchedHandler OnPublicIpAddressFetched;

    class PublicIpAddress
    {
        public string Ip { get; set; }
    }

    [Header("Multi-Lobby Settings")]
    [SerializeField] private string publicIpApiUrl = "https://api.ipify.org/?format=json";
    [SerializeField] private string dbConnectionString = "localhost:6379";
    [SerializeField] private int secondsBetweenHeartbeats = 30;
    [Space]
    [SerializeField] private int maxSecondsBeforeUnverifiedUserRemoval = 15;
    [SerializeField] private int tokenValidityDurationSeconds = 120;
    public ServerDatabaseHandler Database { get; private set; }
    public ServerTokenVerifier TokenVerifier { get; private set; }
#endif

    private List<NetTransport> transports = new List<NetTransport>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Multiple instances of ServerManager detected. Destroying duplicate instance.");
            Destroy(gameObject);
            return;
        }

        Debug.Log("<color=green><b>CNS</b></color>: Initializing Server...");

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetResources.Instance.GameMode != GameMode.Singleplayer)
        {
            OnPublicIpAddressFetched += async (ip) =>
            {
                await InitServerAsync(ip);
            };
            StartCoroutine(GetPublicIpAddress());
        }
#endif

        Debug.Log("<color=green><b>CNS</b></color>: Server initialized.");
    }

    public void SendToUser(UserData user, NetPacket packet, TransportMethod method)
    {
        if (packet != null)
        {
            var (remoteId, transportIndex) = GetRemoteIdAndTransportIndex(user);
            transports[transportIndex].Send(remoteId, packet, method);
        }
    }

    public void SendToUsers(List<UserData> users, NetPacket packet, TransportMethod method)
    {
        if (packet != null)
        {
            Dictionary<NetTransport, List<uint>> userDict = new Dictionary<NetTransport, List<uint>>();
            foreach (var user in users)
            {
                var (remoteId, transportIndex) = GetRemoteIdAndTransportIndex(user);
                if (!userDict.ContainsKey(transports[transportIndex]))
                {
                    userDict[transports[transportIndex]] = new List<uint>();
                }
                userDict[transports[transportIndex]].Add(remoteId);
            }

            foreach (var (transport, remoteIds) in userDict)
            {
                transport.SendToList(remoteIds, packet, method);
            }
        }
    }

    public void BroadcastMessage(NetPacket packet, TransportMethod method)
    {
        foreach (NetTransport transport in transports)
        {
            transport.SendToAll(packet, method);
        }
    }

    public async void KickUser(UserData user)
    {
        if (ServerData.ConnectedUsers.TryGetValue(user.UserId, out UserData userData))
        {
            await RemoveUser(userData);
        }

        var (remoteId, transportIndex) = GetRemoteIdAndTransportIndex(user);
        transports[transportIndex].DisconnectRemote(remoteId);
    }

    private async void HandleNetworkConnected(NetTransport transport, ConnectedArgs args)
    {
        ulong userId = CreateCombinedId(args.RemoteId, transport);
        try
        {
            if (!ServerData.ConnectedUsers.ContainsKey(userId))
            {
                await RegisterUser(userId);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User with ID {userId} attempted to connect again.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error handling connection for user {userId}: {ex.Message}");
        }
    }

    private async void HandleNetworkDisconnected(NetTransport transport, DisconnectedArgs args)
    {
        ulong userId = CreateCombinedId(args.RemoteId, transport);
        try
        {
            if (ServerData.ConnectedUsers.TryGetValue(userId, out UserData userData))
            {
                await RemoveUser(userData);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error handling disconnection for user {userId}: {ex.Message}");
        }
    }

    private async void HandleNetworkReceived(NetTransport transport, ReceivedArgs args)
    {
        ulong userId = CreateCombinedId(args.RemoteId, transport);
        if (ServerData.ConnectedUsers.TryGetValue(userId, out UserData remoteUser))
        {
#if !UNITY_EDITOR
            try
            {
#endif
            if (remoteUser.InLobby && ServerData.ActiveLobbies.TryGetValue(remoteUser.LobbyId, out ServerLobby existingLobby))
            {
                existingLobby.ReceiveData(remoteUser, args.Packet, args.TransportMethod);
            }
            else
            {
                NetPacket packet = args.Packet;
                ServiceType serviceType = (ServiceType)packet.ReadByte();
                CommandType commandType = (CommandType)packet.ReadByte();
                if (serviceType == ServiceType.CONNECTION && commandType == CommandType.CONNECTION_REQUEST)
                {
                    ConnectionData connectionData = GetConnectionData(packet);
                    if (connectionData == null)
                    {
                        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Invalid connection data received from user {userId}.");
                        KickUser(remoteUser);
                        return;
                    }

                    ServerLobby newLobby = await GetLobbyData(connectionData);
                    if (newLobby == null)
                    {
                        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Lobby {connectionData.LobbyId} does not exist. User {userId} cannot join.");
                        SendToUser(remoteUser, PacketBuilder.ConnectionResponse(false, connectionData.LobbyId, LobbyRejectionType.LobbyNotFound), TransportMethod.Reliable);
                        KickUser(remoteUser);
                        return;
                    }

                    if (newLobby.LobbyData.UserCount >= connectionData.LobbySettings.MaxUsers)
                    {
                        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Lobby {connectionData.LobbyId} is full. User {userId} cannot join.");
                        SendToUser(remoteUser, PacketBuilder.ConnectionResponse(false, connectionData.LobbyId, LobbyRejectionType.LobbyFull), TransportMethod.Reliable);
                        KickUser(remoteUser);
                        return;
                    }

                    SendToUser(remoteUser, PacketBuilder.ConnectionResponse(true, connectionData.LobbyId), TransportMethod.Reliable);
                    await AddUserToLobby(remoteUser, newLobby, connectionData);
                }
                else
                {
                    Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User {userId} is not in any active lobby.");
                    KickUser(remoteUser);
                }
            }
#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error when processing received data from user {userId}: {ex.Message}");
                KickUser(remoteUser);
            }
#endif
        }
    }

    void FixedUpdate()
    {
        Physics.simulationMode = SimulationMode.Script;
        foreach (ServerLobby serverLobby in ServerData.ActiveLobbies.Values)
        {
            serverLobby.Tick();
        }
        //Physics.simulationMode = SimulationMode.FixedUpdate;

        ServerTick++;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    async void OnDestroy()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        RemoveTransports();

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetResources.Instance.GameMode != GameMode.Singleplayer)
        {
            await Database.Close();
        }
#endif
    }

    private ConnectionData GetConnectionData(NetPacket packet)
    {
        if (NetResources.Instance.GameMode == GameMode.Singleplayer)
        {
            return new ConnectionData().Deserialize(packet);
        }

        ConnectionData connectionData = null;
#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        connectionData = TokenVerifier.VerifyToken(packet.ReadString());
#elif CNS_SERVER_SINGLE || CNS_SYNC_HOST
        connectionData = new ConnectionData().Deserialize(packet);
#else
        connectionData = null;
#endif

#if CNS_SERVER_SINGLE && CNS_LOBBY_MULTIPLE && CNS_SYNC_DEDICATED
        connectionData.LobbyId = connectionData.LobbyConnectionType == LobbyConnectionType.Create ? GenerateLobbyId() : connectionData.LobbyId;
#elif CNS_SERVER_SINGLE && CNS_LOBBY_SINGLE
        connectionData.LobbyConnectionType = LobbyConnectionType.Join;
        connectionData.LobbyId = NetResources.Instance.DefaultLobbyId;
        connectionData.LobbySettings = NetResources.Instance.DefaultLobbySettings;
#endif
        return connectionData;
    }

    private async Task<ServerLobby> GetLobbyData(ConnectionData connectionData)
    {
        ServerLobby lobby = null;
#if CNS_LOBBY_SINGLE
        if (ServerData.ActiveLobbies.ContainsKey(connectionData.LobbyId))
        {
            lobby = ServerData.ActiveLobbies[connectionData.LobbyId];
        }
        else
        {
            lobby = await RegisterLobby(connectionData);
        }
#elif CNS_LOBBY_MULTIPLE
        if (connectionData.LobbyConnectionType == LobbyConnectionType.Join && ServerData.ActiveLobbies.ContainsKey(connectionData.LobbyId))
        {
            lobby = ServerData.ActiveLobbies[connectionData.LobbyId];
            connectionData.LobbySettings = lobby.LobbyData.Settings;
        }
        else if (connectionData.LobbyConnectionType == LobbyConnectionType.Create)
        {
            lobby = await RegisterLobby(connectionData);
        }
#endif
        return lobby;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task<UserData> RegisterUser(ulong userId)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        UserData user = new UserData
        {
            GlobalGuid = Guid.Empty,
            LobbyId = -1,
            UserId = userId,
            Settings = new UserSettings()
            {
                UserName = $"UnverifiedUser_{userId}"
            }
        };
        ServerData.ConnectedUsers[user.UserId] = user;

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetResources.Instance.GameMode != GameMode.Singleplayer)
        {
            await Database.AddUserToServerLimboAsync(user.UserId, maxSecondsBeforeUnverifiedUserRemoval);
            TokenVerifier.AddUnverifiedUser(user);
        }
#endif
        Debug.Log($"<color=green><b>CNS</b></color>: Server registered new user {user.UserId}.");
        return user;
    }

    private async Task RemoveUser(UserData user)
    {
        ServerData.ConnectedUsers.Remove(user.UserId);

        if (ServerData.ActiveLobbies.TryGetValue(user.LobbyId, out ServerLobby lobby))
        {
            await RemoveUserFromLobby(user, lobby);

            if (lobby.LobbyData.LobbyUsers.Count == 0)
            {
                await RemoveLobby(lobby);
            }
            else
            {
                lobby.UserLeft(user);
            }
        }

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetResources.Instance.GameMode != GameMode.Singleplayer)
        {
            await Database.DeleteUserAsync(user.GlobalGuid);
            await Database.RemoveUserFromServerAsync(user.GlobalGuid);
            await Database.RemoveUserFromServerLimboAsync(user.UserId);
            TokenVerifier.RemoveUnverifiedUser(user);
        }
#endif
        Debug.Log($"<color=green><b>CNS</b></color>: Server removed user {user.UserId}.");
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task<ServerLobby> RegisterLobby(ConnectionData connectionData)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        Scene lobbyScene = SceneManager.CreateScene($"Lobby_{connectionData.LobbyId}_Scene", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        Scene previousScene = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(lobbyScene);
        ServerLobby lobby = new GameObject($"Lobby_{connectionData.LobbyId}").AddComponent<ServerLobby>();
        lobby.Init(connectionData.LobbyId, lobbyScene);
        lobby.LobbyData.Settings = connectionData.LobbySettings;
        ServerData.ActiveLobbies.Add(lobby.LobbyData.LobbyId, lobby);
        SceneManager.SetActiveScene(previousScene);

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetResources.Instance.GameMode != GameMode.Singleplayer)
        {
            await Database.SaveLobbyMetadataAsync(lobby.LobbyData);
            await Database.RemoveLobbyFromLimbo(lobby.LobbyData.LobbyId);
            await Database.AddLobbyToServerAsync(lobby.LobbyData.LobbyId);
        } 
#endif
        Debug.Log($"<color=green><b>CNS</b></color>: Server registered new lobby {lobby.LobbyData.LobbyId}.");
        return lobby;
    }

    private async Task RemoveLobby(ServerLobby lobby)
    {
        ServerData.ActiveLobbies.Remove(lobby.LobbyData.LobbyId);
        Destroy(lobby.gameObject);
        await SceneManager.UnloadSceneAsync(lobby.LobbyScene);

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetResources.Instance.GameMode != GameMode.Singleplayer)
        {
            await Database.RemoveLobbyFromServerAsync(lobby.LobbyData.LobbyId);
            await Database.DeleteLobbyAsync(lobby.LobbyData.LobbyId);
        }
#endif
        Debug.Log($"<color=green><b>CNS</b></color>: Server removed lobby {lobby.LobbyData.LobbyId}.");
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task AddUserToLobby(UserData user, ServerLobby lobby, ConnectionData connectionData)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        user.LobbyId = connectionData.LobbyId;
        user.GlobalGuid = connectionData.UserGuid;
        user.Settings = connectionData.UserSettings;
        lobby.LobbyData.LobbyUsers.Add(user);

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetResources.Instance.GameMode != GameMode.Singleplayer)
        {
            TokenVerifier.RemoveUnverifiedUser(user);
            await Database.SaveUserMetadataAsync(user);
            await Database.RemoveUserFromServerLimboAsync(user.UserId);
            await Database.AddUserToServerAsync(user.GlobalGuid);
            await Database.AddUserToLobbyAsync(connectionData.LobbyId, user.GlobalGuid);
        }
#endif
        Debug.Log($"<color=green><b>CNS</b></color>: User {user.UserId} joined lobby {lobby.LobbyData.LobbyId}.");
        lobby.UserJoined(user);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task RemoveUserFromLobby(UserData user, ServerLobby lobby)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        lobby.LobbyData.LobbyUsers.Remove(user);

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetResources.Instance.GameMode != GameMode.Singleplayer)
        {
            await Database.RemoveUserFromLobbyAsync(user.LobbyId, user.GlobalGuid);
        }
#endif
        Debug.Log($"<color=green><b>CNS</b></color>: User {user.UserId} left lobby {lobby.LobbyData.LobbyId}.");
    }

    public void RegisterTransport(TransportType transportType)
    {
        NetTransport transport = Instantiate(NetResources.Instance.TransportPrefabs[transportType], this.transform).GetComponent<NetTransport>();
        transports.Add(transport);
        AddTransportEvents(transport);
        transport.Initialize(NetDeviceType.Server);
        transport.StartDevice();
    }

    public void AddTransport(NetTransport transport)
    {
        transports.Add(transport);
        transport.transform.SetParent(this.transform);
        AddTransportEvents(transport);
    }

    public void AddTransportEvents(NetTransport transport)
    {
        transport.OnNetworkConnected += HandleNetworkConnected;
        transport.OnNetworkDisconnected += HandleNetworkDisconnected;
        transport.OnNetworkReceived += HandleNetworkReceived;
    }

    public void ClearTransportEvents(NetTransport transport)
    {
        transport.OnNetworkConnected -= HandleNetworkConnected;
        transport.OnNetworkDisconnected -= HandleNetworkDisconnected;
        transport.OnNetworkReceived -= HandleNetworkReceived;
    }

    public void RemoveTransports()
    {
        foreach (NetTransport transport in transports)
        {
            ClearTransportEvents(transport);
            transport.Shutdown();
            Destroy(transport.gameObject);
        }
        transports.Clear();
    }

    private (uint, int) GetRemoteIdAndTransportIndex(UserData user)
    {
        return ((uint)user.UserId, (int)(user.UserId >> 32));
    }

    private ulong CreateCombinedId(uint userId, NetTransport transport)
    {
        return (ulong)transports.IndexOf(transport) << 32 | userId;
    }

#if CNS_SERVER_SINGLE && CNS_LOBBY_MULTIPLE && CNS_SYNC_DEDICATED
    private int GenerateLobbyId()
    {
        int newLobbyId;
        do
        {
            newLobbyId = UnityEngine.Random.Range(100000, 1000000);
        } while (ServerData.ActiveLobbies.ContainsKey(newLobbyId));
        return newLobbyId;
    }
#endif

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
    private IEnumerator GetPublicIpAddress()
    {
        using (var www = UnityWebRequest.Get(publicIpApiUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var json = www.downloadHandler.text;
                var address = JsonConvert.DeserializeObject<PublicIpAddress>(json).Ip;
                Debug.Log($"<color=green><b>CNS</b></color>: Server's public IP Address: {address}");
                OnPublicIpAddressFetched?.Invoke(address);
            }
            else
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: Failed to fetch public IP Address: {www.error}");
            }
        }
    }

    private async Task InitServerAsync(string address)
    {
        ServerData.Settings.ServerId = Guid.NewGuid();
        ServerData.Settings.ServerKey = GenerateSecretKey();
        ServerData.Settings.ServerAddress = address;

        await InitDatabaseAsync();
        InitTokenVerifier();
    }

    private async Task InitDatabaseAsync()
    {
        Database = new ServerDatabaseHandler();
        await Database.Connect(dbConnectionString, ServerData.Settings.ServerId);
        await Database.SaveServerMetadataAsync(ServerData);
        Database.StartHeartbeat(secondsBetweenHeartbeats);
    }

    private void InitTokenVerifier()
    {
        TokenVerifier = new ServerTokenVerifier(ServerData.Settings.ServerKey);
        TokenVerifier.StartUnverifiedUserCleanup(maxSecondsBeforeUnverifiedUserRemoval);
        TokenVerifier.StartTokenCleanup(tokenValidityDurationSeconds, 10);
    }

    private string GenerateSecretKey(int byteLength = 32)
    {
        var keyBytes = new byte[byteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }

        return Convert.ToBase64String(keyBytes);
    }
#endif
}
