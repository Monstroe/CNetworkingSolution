using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MultiTransportUtility))]
public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance { get; private set; }
    public ServerData ServerData { get; private set; } = new ServerData();

    [Header("General Settings")]
    [SerializeField] private int minLobbyId = 1000;
    [SerializeField] private int maxLobbyId = 10000;
    [SerializeField] private bool spawnLobbiesOnStart = false;

    [Header("Connection Settings")]
    [SerializeField] private int maxSecondsBeforeUnverifiedUserRemoval = 30;

    [Header("Lobby Settings")]
    [SerializeField] private ServerLobby lobbyPrefab;

    private readonly ConnectionRequestedEventBus connectionRequestedEventBus = new ConnectionRequestedEventBus();
    private readonly ConnectionLostEventBus connectionLostEventBus = new ConnectionLostEventBus();
    private readonly ConnectionErrorEventBus connectionErrorEventBus = new ConnectionErrorEventBus();
    private MultiTransportUtility transportUtility;

#if UNITY_EDITOR
    void OnValidate()
    {
        minLobbyId = Mathf.Max(0, minLobbyId);
        maxLobbyId = Mathf.Max(minLobbyId + 1, maxLobbyId);
        maxSecondsBeforeUnverifiedUserRemoval = Mathf.Max(1, maxSecondsBeforeUnverifiedUserRemoval);
    }
#endif

    async void Awake()
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
        ServerData.ServerId = GenerateUniqueId();
        ServerData.SecretKey = GenerateSecretKey();

        if (spawnLobbiesOnStart)
        {
            for (int i = minLobbyId; i < maxLobbyId; i++)
            {
                _ = await RegisterLobby(null, i);
            }
        }
    }

    void Start()
    {
        Debug.Log("<color=green><b>CNS</b></color>: Server initialized.");
    }

    void Update()
    {
        foreach (var (userId, connectionEvent) in ServerData.ConnectingUsers)
        {
            if (DateTime.UtcNow - connectionEvent.ConnectionTime > TimeSpan.FromSeconds(maxSecondsBeforeUnverifiedUserRemoval))
            {
                transportUtility.KickRemote(userId);
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User {userId} took too long to send connection data and was removed.");
            }
        }
    }

    void FixedUpdate()
    {
        Physics.simulationMode = SimulationMode.Script;
        foreach (ServerLobby serverLobby in ServerData.ActiveLobbies.Values)
        {
            serverLobby.Tick();
        }
    }

    void OnDestroy()
    {
        transportUtility.RemoveTransports();
        ClearTransportUtilityEvents();
    }

    private async void HandleNetworkConnected(ulong remoteId)
    {
#if !UNITY_EDITOR
        try
        {
#endif
        if (!ServerData.ConnectedUsers.ContainsKey(remoteId) && !ServerData.ConnectingUsers.ContainsKey(remoteId))
        {
            await RegisterUser(remoteId);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User with ID {remoteId} attempted to connect again.");
        }
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error handling connection for user {remoteId}: {ex.Message}");
        }
#endif
    }

    private async void HandleNetworkDisconnected(ulong remoteId, TransportCode code)
    {
#if !UNITY_EDITOR
        try
        {
#endif
        if (ServerData.ConnectedUsers.TryGetValue(remoteId, out UserData userData))
        {
            ServerData.RemoveConnectedUser(userData.UserId);
            await RemoveUser(userData);
        }
        else if (ServerData.ConnectingUsers.TryGetValue(remoteId, out ConnectionRequestedEventResult connectionEvtResult))
        {
            ServerData.RemoveConnectingUser(connectionEvtResult.ConnectingUser.UserId);
            await RemoveUser(connectionEvtResult.ConnectingUser);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User with ID {remoteId} already disconnected.");
        }
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error handling disconnection for user {remoteId}: {ex.Message}");
        }
#endif
    }

    private async void HandleNetworkReceived(ulong remoteId, NetPacket packet, TransportMethod? method)
    {
#if !UNITY_EDITOR
        try
        {
#endif
        if (ServerData.ConnectedUsers.TryGetValue(remoteId, out UserData remoteUser) && ServerData.ActiveLobbies.TryGetValue(remoteUser.LobbyId, out ServerLobby existingLobby))
        {
            existingLobby.ReceiveData(remoteUser, packet, method);
        }
        else if (ServerData.ConnectingUsers.TryGetValue(remoteId, out ConnectionRequestedEventResult connectionEvtResult) && packet.ReadEnum<ConnectionCommandType>() == ConnectionCommandType.CONNECTION_REQUEST)
        {
            remoteUser = connectionEvtResult.ConnectingUser;
            ConnectionData connectionData = await GetConnectionData(connectionEvtResult, packet);
            if (connectionData == null)
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Invalid connection data received from user {remoteId}.");
                transportUtility.KickRemote(remoteUser.UserId);
                return;
            }

            ServerLobby newLobby = await GetLobbyData(connectionEvtResult, connectionData);
            if (newLobby == null)
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User {remoteId} could not join lobby {connectionData.LobbyId}.");
                transportUtility.KickRemote(remoteUser.UserId);
                return;
            }

            if (!await AddUserToLobby(connectionEvtResult, newLobby, connectionData))
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User {remoteId} was denied joining lobby {newLobby.LobbyData.LobbyId}.");
                transportUtility.KickRemote(remoteUser.UserId);
                return;
            }

            ServerData.AddConnectedUser(connectionEvtResult.ConnectingUser);
            ServerData.RemoveConnectingUser(remoteId);
            transportUtility.SendToRemote(remoteUser.UserId, ConnectionPacketBuilder.ConnectionResponse(true, ConnectionPacketBuilder.ConnectionData(newLobby.LobbyData.LobbyId, connectionEvtResult.ResponsePacket)), TransportMethod.Reliable);
            newLobby.UserJoined(remoteUser);
            newLobby.LateUserJoined(remoteUser);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received data from unknown user {remoteId}. Data will be ignored.");
            transportUtility.KickRemote(remoteId);
        }
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error when processing received data from user {remoteId}: {ex.Message}");
            transportUtility.KickRemote(remoteId);
        }
#endif
    }

    private void HandleNetworkReceivedUnconnected(IPEndPoint iPEndPoint, NetPacket packet)
    {
#if !UNITY_EDITOR
        try
        {
#endif

        if (maxLobbyId - minLobbyId == 1 && ServerData.ActiveLobbies.TryGetValue(minLobbyId, out ServerLobby lobby))
        {
            lobby.ReceiveDataUnconnected(iPEndPoint, packet);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received unconnected packet from {iPEndPoint}. This is not supported on this server configuration and the packet will be ignored.");
        }
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error when processing unconnected received data from {iPEndPoint}: {ex.Message}");
        }
#endif


        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received unconnected packet from {iPEndPoint}. This is not supported on the server and the packet will be ignored.");
    }

    private async void HandleNetworkError(TransportCode code, SocketError? socketError)
    {
        _ = await connectionErrorEventBus.Fire(new ConnectionErrorEvent()
        {
            Code = code,
            SocketError = socketError
        });

        Debug.LogError($"<color=red><b>CNS</b></color>: Network error occurred: {code} {(socketError.HasValue ? $"(Socket Error: {socketError.Value})" : "")}");
    }

    private async Task<ConnectionData> GetConnectionData(ConnectionRequestedEventResult connectingEvtData, NetPacket packet)
    {
        ConnectionData connectionData = new ConnectionData().Deserialize(packet);
        connectionData.LobbyId = connectionData.LobbyConnectionType == LobbyConnectionType.Create || (connectionData.LobbyConnectionType == LobbyConnectionType.JoinOrCreate && (connectionData.LobbyId > maxLobbyId || connectionData.LobbyId < minLobbyId)) ? GenerateLobbyId() : connectionData.LobbyId;

        connectingEvtData = await connectionRequestedEventBus.Fire(new ConnectionDataReceivedEvent()
        {
            ConnectionData = connectionData,
            ConnectingUser = connectingEvtData.ConnectingUser,
            ConnectionTime = connectingEvtData.ConnectionTime,
            ResponsePacket = connectingEvtData.ResponsePacket
        });
        if (connectingEvtData.UserRejected)
        {
            transportUtility.SendToRemote(connectingEvtData.ConnectingUser.UserId, ConnectionPacketBuilder.ConnectionResponse(false, connectingEvtData.ResponsePacket), TransportMethod.Reliable);
            return null;
        }

        return connectionData;
    }

    private async Task<ServerLobby> GetLobbyData(ConnectionRequestedEventResult connectingEvtData, ConnectionData connectionData)
    {
        ServerLobby lobby = null;

        if (connectionData.LobbyId < minLobbyId || connectionData.LobbyId > maxLobbyId)
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User {connectingEvtData.ConnectingUser.UserId} attempted to connect with invalid lobby ID {connectionData.LobbyId}.");
            return null;
        }

        if ((connectionData.LobbyConnectionType == LobbyConnectionType.Create || connectionData.LobbyConnectionType == LobbyConnectionType.JoinOrCreate) && !ServerData.ActiveLobbies.TryGetValue(connectionData.LobbyId, out lobby))
        {
            lobby = await RegisterLobby(connectingEvtData, connectionData.LobbyId);
        }
        else if (connectionData.LobbyConnectionType == LobbyConnectionType.JoinIfExists && !ServerData.ActiveLobbies.TryGetValue(connectionData.LobbyId, out lobby))
        {
            connectingEvtData = await connectionRequestedEventBus.Fire(new LobbyNotFoundEvent()
            {
                LobbyId = connectionData.LobbyId,
                ConnectingUser = connectingEvtData.ConnectingUser,
                ConnectionTime = connectingEvtData.ConnectionTime,
                UserRejected = true,
                ResponsePacket = connectingEvtData.ResponsePacket
            });
            transportUtility.SendToRemote(connectingEvtData.ConnectingUser.UserId, ConnectionPacketBuilder.ConnectionResponse(false, connectingEvtData.ResponsePacket), TransportMethod.Reliable);
        }

        return lobby;
    }

    private async Task RegisterUser(ulong userId)
    {
        UserData user = new UserData()
        {
            GlobalGuid = GenerateUniqueId(),
            LobbyId = -1,
            UserId = userId
        };

        var result = await connectionRequestedEventBus.Fire(new UserRegisteredEvent
        {
            ConnectingUser = user,
            ConnectionTime = DateTime.UtcNow
        });
        if (result.UserRejected)
        {
            transportUtility.SendToRemote(user.UserId, ConnectionPacketBuilder.ConnectionResponse(false, result.ResponsePacket), TransportMethod.Reliable);
            transportUtility.KickRemote(user.UserId);
        }

        ServerData.AddConnectingUser(result);
        Debug.Log($"<color=green><b>CNS</b></color>: Server registered new user {userId}.");
    }

    private async Task RemoveUser(UserData user)
    {
        if (ServerData.ActiveLobbies.TryGetValue(user.LobbyId, out ServerLobby lobby))
        {
            await RemoveUserFromLobby(user, lobby);

            if (lobby.LobbyData.LobbyUsers.Count == 0 && !spawnLobbiesOnStart)
            {
                await RemoveLobby(user, lobby);
            }
            else
            {
                if (user.InGame)
                {
                    lobby.UserLeftGame(user);
                    lobby.LateUserLeftGame(user);
                }
                lobby.UserLeft(user);
                lobby.LateUserLeft(user);
            }
        }

        _ = await connectionLostEventBus.Fire(new UserRemovedEvent()
        {
            DisconnectingUser = user
        });

        Debug.Log($"<color=green><b>CNS</b></color>: Server removed user {user.UserId}.");
    }

    private async Task<ServerLobby> RegisterLobby(ConnectionRequestedEventResult connectingEvtData, int lobbyId)
    {
        Scene lobbyScene = SceneManager.CreateScene($"Lobby_{lobbyId}_Scene", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        Scene previousScene = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(lobbyScene);
        ServerLobby lobby = Instantiate(lobbyPrefab.gameObject).GetComponent<ServerLobby>();
        lobby.name = $"Lobby_{lobbyId}";
        lobby.Init(transportUtility, lobbyScene);
        lobby.LobbyData.LobbyId = lobbyId;
        ServerData.AddLobby(lobby);
        SceneManager.SetActiveScene(previousScene);

        if (connectingEvtData != null)
        {
            connectingEvtData = await connectionRequestedEventBus.Fire(new LobbyRegisteredEvent()
            {
                Lobby = lobby,
                ConnectingUser = connectingEvtData.ConnectingUser,
                ConnectionTime = connectingEvtData.ConnectionTime,
                ResponsePacket = connectingEvtData.ResponsePacket
            });
            if (connectingEvtData.UserRejected)
            {
                transportUtility.SendToRemote(connectingEvtData.ConnectingUser.UserId, ConnectionPacketBuilder.ConnectionResponse(false, connectingEvtData.ResponsePacket), TransportMethod.Reliable);
                await RemoveLobby(connectingEvtData.ConnectingUser, lobby);
                return null;
            }
        }

        Debug.Log($"<color=green><b>CNS</b></color>: Server registered new lobby {lobby.LobbyData.LobbyId}.");
        return lobby;
    }

    private async Task RemoveLobby(UserData disconnectingUser, ServerLobby lobby)
    {
        ServerData.RemoveLobby(lobby.LobbyData.LobbyId);

        _ = await connectionLostEventBus.Fire(new LobbyRemovedEvent()
        {
            Lobby = lobby,
            DisconnectingUser = disconnectingUser
        });

        Destroy(lobby.gameObject);
        if (lobby.LobbyScene.HasValue)
        {
            await SceneManager.UnloadSceneAsync(lobby.LobbyScene.Value);
        }

        Debug.Log($"<color=green><b>CNS</b></color>: Server removed lobby {lobby.LobbyData.LobbyId}.");
    }

    private async Task<bool> AddUserToLobby(ConnectionRequestedEventResult connectingEvtData, ServerLobby lobby, ConnectionData connectionData)
    {
        connectingEvtData.ConnectingUser.LobbyId = connectionData.LobbyId;
        lobby.LobbyData.AddUser(connectingEvtData.ConnectingUser);

        var result = await connectionRequestedEventBus.Fire(new UserAddedToLobbyEvent
        {
            Lobby = lobby,
            ConnectingUser = connectingEvtData.ConnectingUser,
            ConnectionTime = connectingEvtData.ConnectionTime,
            ResponsePacket = connectingEvtData.ResponsePacket
        });
        if (result.UserRejected)
        {
            connectingEvtData.ConnectingUser.LobbyId = -1;
            lobby.LobbyData.RemoveUser(connectingEvtData.ConnectingUser);
            transportUtility.SendToRemote(connectingEvtData.ConnectingUser.UserId, ConnectionPacketBuilder.ConnectionResponse(false, result.ResponsePacket), TransportMethod.Reliable);
            return false;
        }

        Debug.Log($"<color=green><b>CNS</b></color>: User {connectingEvtData.ConnectingUser.UserId} joined lobby {lobby.LobbyData.LobbyId}.");
        return true;
    }

    private async Task RemoveUserFromLobby(UserData user, ServerLobby lobby)
    {
        lobby.LobbyData.RemoveUser(user);

        _ = await connectionLostEventBus.Fire(new UserRemovedFromLobbyEvent
        {
            Lobby = lobby,
            DisconnectingUser = user
        });

        Debug.Log($"<color=green><b>CNS</b></color>: User {user.UserId} left lobby {lobby.LobbyData.LobbyId}.");
    }

    public void RegisterConnectionEventListener(INetEvent listener)
    {
        connectionRequestedEventBus.RegisterListener(listener);
        connectionLostEventBus.RegisterListener(listener);
        connectionErrorEventBus.RegisterListener(listener);
    }

    public void UnregisterConnectionEventListener(INetEvent listener)
    {
        connectionRequestedEventBus.UnregisterListener(listener);
        connectionLostEventBus.UnregisterListener(listener);
        connectionErrorEventBus.UnregisterListener(listener);
    }

    public void StartTransports()
    {
        transportUtility.StartTransports();
    }

    public void RegisterTransport<T>() where T : NetTransport
    {
        transportUtility.RegisterTransport<T>(NetDeviceType.Server);
    }

    public void AddTransport(NetTransport transport)
    {
        transportUtility.AddTransport(transport);
    }

    public void RemoveTransport<T>() where T : NetTransport
    {
        NetTransport transport = transportUtility.Transports.Find(t => t is T);
        transportUtility.RemoveTransport(transport);
    }

    public void RemoveTransports()
    {
        transportUtility.RemoveTransports();
    }

    private void AddTransportUtilityEvents()
    {
        transportUtility.OnMultiConnected += HandleNetworkConnected;
        transportUtility.OnMultiDisconnected += HandleNetworkDisconnected;
        transportUtility.OnMultiReceived += HandleNetworkReceived;
        transportUtility.OnMultiReceivedUnconnected += HandleNetworkReceivedUnconnected;
        transportUtility.OnMultiError += HandleNetworkError;
    }

    private void ClearTransportUtilityEvents()
    {
        transportUtility.OnMultiConnected -= HandleNetworkConnected;
        transportUtility.OnMultiDisconnected -= HandleNetworkDisconnected;
        transportUtility.OnMultiReceived -= HandleNetworkReceived;
        transportUtility.OnMultiReceivedUnconnected -= HandleNetworkReceivedUnconnected;
        transportUtility.OnMultiError -= HandleNetworkError;
    }

    private int GenerateLobbyId()
    {
        int newLobbyId;
        int attempts = 0;
        do
        {
            newLobbyId = UnityEngine.Random.Range(minLobbyId, maxLobbyId);
            attempts++;
        } while (ServerData.ActiveLobbies.ContainsKey(newLobbyId) && attempts < (maxLobbyId - minLobbyId));
        return newLobbyId;
    }

    private Guid GenerateUniqueId()
    {
        return Guid.NewGuid();
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
}

public class UserRegisteredEvent : ConnectionRequestedEvent { }

public class ConnectionDataReceivedEvent : ConnectionRequestedEvent
{
    public ConnectionData ConnectionData { get; internal set; }
}

public class LobbyNotFoundEvent : ConnectionRequestedEvent
{
    public int LobbyId { get; internal set; }
}

public class LobbyRegisteredEvent : ConnectionRequestedEvent
{
    public ServerLobby Lobby { get; internal set; }
}

public class UserAddedToLobbyEvent : ConnectionRequestedEvent
{
    public ServerLobby Lobby { get; internal set; }
}

public class UserRemovedEvent : ConnectionLostEvent { }

public class LobbyRemovedEvent : ConnectionLostEvent
{
    public ServerLobby Lobby { get; internal set; }
}

public class UserRemovedFromLobbyEvent : ConnectionLostEvent
{
    public ServerLobby Lobby { get; internal set; }
}
