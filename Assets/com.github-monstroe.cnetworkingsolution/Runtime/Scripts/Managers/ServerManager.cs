using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MultiTransportUtility))]
public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance { get; private set; }
    public ServerData ServerData { get; private set; } = new ServerData();
    public NetMode NetMode { get; set; }

    [Header("Lobby Settings")]
    [SerializeField] private ServerLobby lobbyPrefab;

#if CNS_LOBBY_SINGLE
    [Header("Single-Lobby Settings")]
    [SerializeField] private bool spawnLobbyOnStart = false;
#endif

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
    public delegate void PublicIpAddressFetchedHandler(string ipAddress);
    public event PublicIpAddressFetchedHandler OnPublicIpAddressFetched;

    class PublicIpAddress
    {
        public string Ip { get; set; }
    }

    [Header("Multi-Server Settings")]
    [SerializeField] private string publicIpApiUrl = "https://api.ipify.org/?format=json";
    [SerializeField] private string dbConnectionString = "localhost:6379";
    [SerializeField] private int secondsBetweenHeartbeats = 30;
    [Space]
    [SerializeField] private int maxSecondsBeforeUnverifiedUserRemoval = 15;
    [SerializeField] private int tokenValidityDurationSeconds = 120;
    public ServerDatabaseHandler Database { get; private set; }
    public ServerTokenVerifier TokenVerifier { get; private set; }
#endif

    private MultiTransportUtility transportUtility;
    private Action onInitialized;
    private bool initialized = false;

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

        transportUtility = GetComponent<MultiTransportUtility>();
        AddTransportUtilityEvents();
        ServerData.ServerId = Guid.NewGuid();
        ServerData.SecretKey = GenerateSecretKey();
        NetMode = NetResources.Instance.DefaultNetMode;

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetMode != NetMode.Local)
        {
            OnPublicIpAddressFetched += async (ip) =>
            {
                await InitServerAsync(ip);
            };
            StartCoroutine(GetPublicIpAddress());
        }
#else
        initialized = true;
#endif
    }

    void Start()
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        onInitialized = new Action(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
#if CNS_LOBBY_SINGLE
            if (spawnLobbyOnStart)
            {
                await RegisterLobby(new ConnectionData
                {
                    LobbyId = NetResources.Instance.DefaultLobbyId,
                    LobbyConnectionType = LobbyConnectionType.Create,
                    LobbySettings = NetResources.Instance.DefaultLobbySettings.Clone(),
                    UserGuid = Guid.Empty,
                    UserSettings = new UserSettings()
                });
            }
#endif
            Debug.Log("<color=green><b>CNS</b></color>: Server initialized.");
        });

        StartCoroutine(ServerInitialized());
    }

    private IEnumerator ServerInitialized()
    {
        yield return new WaitUntil(() => initialized);
        onInitialized?.Invoke();
    }

    public void KickUser(UserData user)
    {
        transportUtility.KickRemote(user.UserId);
    }

    public void RemoveTransport(TransportType transportType)
    {
        NetTransport transport = transportUtility.Transports.Find(t => t.TransportData.TransportType == transportType);
        transportUtility.RemoveTransport(transport);
    }

    public void RemoveTransports()
    {
        transportUtility.RemoveTransports();
    }

    private async void HandleNetworkConnected(ulong remoteId)
    {
        try
        {
            if (!ServerData.ConnectedUsers.ContainsKey(remoteId))
            {
                await RegisterUser(remoteId);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User with ID {remoteId} attempted to connect again.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error handling connection for user {remoteId}: {ex.Message}");
        }
    }

    private async void HandleNetworkDisconnected(ulong remoteId, TransportCode code)
    {
        try
        {
            if (ServerData.ConnectedUsers.TryGetValue(remoteId, out UserData userData))
            {
                await RemoveUser(userData);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User with ID {remoteId} already disconnected.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error handling disconnection for user {remoteId}: {ex.Message}");
        }
    }

    private async void HandleNetworkReceived(ulong remoteId, NetPacket packet, TransportMethod? method)
    {
        if (ServerData.ConnectedUsers.TryGetValue(remoteId, out UserData remoteUser))
        {
#if !UNITY_EDITOR
            try
            {
#endif
            if (remoteUser.InLobby && ServerData.ActiveLobbies.TryGetValue(remoteUser.LobbyId, out ServerLobby existingLobby))
            {
                existingLobby.ReceiveData(remoteUser, packet, method);
            }
            else
            {
                ServiceType serviceType = (ServiceType)packet.ReadByte();
                CommandType commandType = (CommandType)packet.ReadByte();
                if (serviceType == ServiceType.CONNECTION && commandType == CommandType.CONNECTION_REQUEST)
                {
                    ConnectionData connectionData = GetConnectionData(packet);
                    if (connectionData == null)
                    {
                        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Invalid connection data received from user {remoteId}.");
                        KickUser(remoteUser);
                        return;
                    }

                    ServerLobby newLobby = await GetLobbyData(connectionData);
                    if (newLobby == null)
                    {
                        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Lobby {connectionData.LobbyId} does not exist. User {remoteId} cannot join.");
                        transportUtility.SendToRemote(remoteUser.UserId, PacketBuilder.ConnectionResponse(false, connectionData.LobbyId, LobbyRejectionType.LobbyNotFound), TransportMethod.Reliable);
                        KickUser(remoteUser);
                        return;
                    }

                    if (newLobby.LobbyData.UserCount >= connectionData.LobbySettings.MaxUsers)
                    {
                        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Lobby {connectionData.LobbyId} is full. User {remoteId} cannot join.");
                        transportUtility.SendToRemote(remoteUser.UserId, PacketBuilder.ConnectionResponse(false, connectionData.LobbyId, LobbyRejectionType.LobbyFull), TransportMethod.Reliable);
                        KickUser(remoteUser);
                        return;
                    }

                    transportUtility.SendToRemote(remoteUser.UserId, PacketBuilder.ConnectionResponse(true, connectionData.LobbyId), TransportMethod.Reliable);
                    await AddUserToLobby(remoteUser, newLobby, connectionData);
                }
                else
                {
                    Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User {remoteId} is not in any active lobby.");
                    KickUser(remoteUser);
                }
            }
#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error when processing received data from user {remoteId}: {ex.Message}");
                KickUser(remoteUser);
            }
#endif
        }
    }

#if CNS_LOBBY_SINGLE
    private void HandleNetworkReceivedUnconnected(IPEndPoint iPEndPoint, NetPacket packet)
    {
#if !UNITY_EDITOR
        try
        {
#endif
        ServerData.CurrentLobby.ReceiveDataUnconnected(iPEndPoint, packet);
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error when processing unconnected received data from {iPEndPoint}: {ex.Message}");
        }
#endif
    }
#endif

    private void HandleNetworkError(TransportCode code, SocketError? socketError)
    {
        Debug.LogError($"<color=red><b>CNS</b></color>: Network error occurred: {code} {(socketError.HasValue ? $"(Socket Error: {socketError.Value})" : "")}");
    }

    void FixedUpdate()
    {
        Physics.simulationMode = SimulationMode.Script;
        foreach (ServerLobby serverLobby in ServerData.ActiveLobbies.Values)
        {
            serverLobby.Tick();
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    async void OnDestroy()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        transportUtility.RemoveTransports();
        ClearTransportUtilityEvents();

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetMode != NetMode.Local)
        {
            await Database.Close();
        }
#endif
    }

    private ConnectionData GetConnectionData(NetPacket packet)
    {
        if (NetMode == NetMode.Local)
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
        connectionData.LobbySettings = NetResources.Instance.DefaultLobbySettings.Clone();
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
        if (NetMode != NetMode.Local)
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
#if CNS_LOBBY_SINGLE
                if (spawnLobbyOnStart)
                {
                    lobby.UserLeft(user);
                }
                else 
                {
                    await RemoveLobby(lobby);
                }
#else
                await RemoveLobby(lobby);
#endif
            }
            else
            {
                lobby.UserLeft(user);
            }
        }

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetMode != NetMode.Local)
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
        ServerLobby lobby = Instantiate(lobbyPrefab.gameObject).GetComponent<ServerLobby>();
        lobby.name = $"Lobby_{connectionData.LobbyId}";
        lobby.Init(transportUtility, lobbyScene);
        lobby.LobbyData.LobbyId = connectionData.LobbyId;
        lobby.LobbyData.Settings = connectionData.LobbySettings;
        ServerData.ActiveLobbies.Add(lobby.LobbyData.LobbyId, lobby);
        SceneManager.SetActiveScene(previousScene);

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetMode != NetMode.Local)
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
        if (lobby.LobbyScene.HasValue)
        {
            await SceneManager.UnloadSceneAsync(lobby.LobbyScene.Value);
        }

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (NetMode != NetMode.Local)
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
        if (NetMode != NetMode.Local)
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
        if (NetMode != NetMode.Local)
        {
            await Database.RemoveUserFromLobbyAsync(user.LobbyId, user.GlobalGuid);
        }
#endif
        Debug.Log($"<color=green><b>CNS</b></color>: User {user.UserId} left lobby {lobby.LobbyData.LobbyId}.");
    }

#nullable enable
    public void RegisterTransport(TransportType transportType, TransportSettings? transportSettings = null)
    {
        transportUtility.RegisterTransport(transportType, NetDeviceType.Server, transportSettings);
    }
#nullable disable

    public void AddTransport(NetTransport transport)
    {
        transportUtility.AddTransport(transport);
    }

    private void AddTransportUtilityEvents()
    {
        transportUtility.OnMultiConnected += HandleNetworkConnected;
        transportUtility.OnMultiDisconnected += HandleNetworkDisconnected;
        transportUtility.OnMultiReceived += HandleNetworkReceived;
#if CNS_LOBBY_SINGLE
        transportUtility.OnMultiReceivedUnconnected += HandleNetworkReceivedUnconnected;
#endif
        transportUtility.OnMultiError += HandleNetworkError;
    }

    private void ClearTransportUtilityEvents()
    {
        transportUtility.OnMultiConnected -= HandleNetworkConnected;
        transportUtility.OnMultiDisconnected -= HandleNetworkDisconnected;
        transportUtility.OnMultiReceived -= HandleNetworkReceived;
#if CNS_LOBBY_SINGLE
        transportUtility.OnMultiReceivedUnconnected -= HandleNetworkReceivedUnconnected;
#endif
        transportUtility.OnMultiError -= HandleNetworkError;
    }

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
        ServerData.Settings.ConnectionAddress = address;
        ServerData.Settings.ConnectionPort = NetResources.Instance.DefaultTransportSettings.ConnectionPort;
        ServerData.Settings.ConnectionKey = NetResources.Instance.DefaultTransportSettings.ConnectionKey;

        await InitDatabaseAsync();
        InitTokenVerifier();

        initialized = true;
    }

    private async Task InitDatabaseAsync()
    {
        Database = new ServerDatabaseHandler();
        await Database.Connect(dbConnectionString, ServerData.ServerId);
        await Database.SaveServerMetadataAsync(ServerData);
        Database.StartHeartbeat(secondsBetweenHeartbeats);
    }

    private void InitTokenVerifier()
    {
        TokenVerifier = new ServerTokenVerifier(ServerData.SecretKey);
        TokenVerifier.StartUnverifiedUserCleanup(maxSecondsBeforeUnverifiedUserRemoval, 1);
        TokenVerifier.StartTokenCleanup(tokenValidityDurationSeconds, 10);
    }
#endif

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

    private string GenerateSecretKey(int byteLength = 32)
    {
        var keyBytes = new byte[byteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }

        return Convert.ToBase64String(keyBytes);
    }
}
