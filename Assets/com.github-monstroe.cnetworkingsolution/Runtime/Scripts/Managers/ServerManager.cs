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

    [Header("General Settings")]
    [SerializeField] private int minLobbyId = 1000;
    [SerializeField] private int maxLobbyId = 10000;
    [SerializeField] private bool spawnLobbiesOnStart = false;

    [Header("Connection Settings")]
    [SerializeField] private int maxSecondsBeforeUnverifiedUserRemoval = 30;

    [Header("Lobby Settings")]
    [SerializeField] private ServerLobby lobbyPrefab;

    private ConnectionEventBus connectionEventBus = new ConnectionEventBus();
    private MultiTransportUtility transportUtility;

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
        ServerData.ServerId = GenerateUniqueId();
        ServerData.SecretKey = GenerateSecretKey();
        NetMode = NetResources.Instance.DefaultNetMode;
    }

    async void Start()
    {
        if (spawnLobbiesOnStart)
        {
            for (int i = minLobbyId; i < maxLobbyId; i++)
            {
                _ = await RegisterLobby(null, i);
            }
        }

        Debug.Log("<color=green><b>CNS</b></color>: Server initialized.");
    }

    private async void HandleNetworkConnected(ulong remoteId)
    {
        try
        {
            if (!ServerData.ConnectedUsers.ContainsKey(remoteId) && !ServerData.ConnectingUsers.ContainsKey(remoteId))
            {
                await RegisterUser(remoteId);
                Debug.Log($"<color=green><b>CNS</b></color>: Server registered new user {remoteId}.");
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
                ConnectionCommandType commandType = (ConnectionCommandType)packet.ReadByte();
                if (commandType == ConnectionCommandType.CONNECTION_REQUEST)
                {
                    ConnectionEventResult connectionEvtResult = ServerData.ConnectingUsers[remoteUser.UserId];
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
                        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Lobby {connectionData.LobbyId} does not exist. User {remoteId} cannot join.");
                        transportUtility.KickRemote(remoteUser.UserId);
                        return;
                    }

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
                    transportUtility.SendToRemote(remoteUser.UserId, ConnectionPacketBuilder.ConnectionResponse(true, connectionData.LobbyId), TransportMethod.Reliable);
#elif CNS_SERVER_SINGLE || CNS_SYNC_HOST
                    transportUtility.SendToRemote(remoteUser.UserId, ConnectionPacketBuilder.ConnectionResponse(true, connectionData.LobbyId, ConnectionPacketBuilder.ConnectionUserGuid(remoteUser.GlobalGuid)), TransportMethod.Reliable);
#endif
                    await AddUserToLobby(remoteUser, newLobby, connectionData);
                }
                else
                {
                    Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User {remoteId} is not in any active lobby.");
                    transportUtility.KickRemote(remoteUser.UserId);
                }
            }
#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error when processing received data from user {remoteId}: {ex.Message}");
                transportUtility.KickRemote(remoteUser.UserId);
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

    async void OnDestroy()
    {
        transportUtility.RemoveTransports();
        ClearTransportUtilityEvents();
    }

    private async Task<ConnectionData> GetConnectionData(ConnectionEventResult connectingEvtData, NetPacket packet)
    {
        ConnectionData connectionData = new ConnectionData().Deserialize(packet);
        connectionData.LobbyId = connectionData.LobbyConnectionType == LobbyConnectionType.Create ? GenerateLobbyId() : connectionData.LobbyId;

        connectingEvtData = await connectionEventBus.Fire(new ConnectionDataReceivedEvent(connectionData)
        {
            ConnectingUser = connectingEvtData.ConnectingUser,
            ConnectionTime = connectingEvtData.ConnectionTime,
            ResponsePacket = connectingEvtData.ResponsePacket
        });
        if (connectingEvtData.UserDenied)
        {
            transportUtility.SendToRemote(connectingEvtData.ConnectingUser.UserId, ConnectionPacketBuilder.ConnectionResponse(false, connectingEvtData.ResponsePacket), TransportMethod.Reliable);
            return null;
        }

        return connectionData;
    }

    private async Task<ServerLobby> GetLobbyData(ConnectionEventResult connectingEvtData, ConnectionData connectionData)
    {
        ServerLobby lobby = null;

        if (connectionData.LobbyId < minLobbyId || connectionData.LobbyId > maxLobbyId)
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User {connectingEvtData.ConnectingUser.UserId} attempted to connect with invalid lobby ID {connectionData.LobbyId}.");
            return null;
        }

        if ((connectionData.LobbyConnectionType == LobbyConnectionType.Create || connectionData.LobbyConnectionType == LobbyConnectionType.JoinOrCreate) && !ServerData.ActiveLobbies.ContainsKey(connectionData.LobbyId))
        {
            lobby = await RegisterLobby(connectionData.LobbyId);
            connectingEvtData = await connectionEventBus.Fire(new LobbyRegisteredEvent(lobby)
            {
                ConnectingUser = connectingEvtData.ConnectingUser,
                ConnectionTime = connectingEvtData.ConnectionTime,
                ResponsePacket = connectingEvtData.ResponsePacket
            });
            if (connectingEvtData.UserDenied)
            {
                transportUtility.SendToRemote(connectingEvtData.ConnectingUser.UserId, ConnectionPacketBuilder.ConnectionResponse(false, connectingEvtData.ResponsePacket), TransportMethod.Reliable);
                await RemoveLobby(lobby);
                return null;
            }
            Debug.Log($"<color=green><b>CNS</b></color>: Server registered new lobby {lobby.LobbyData.LobbyId}.");
        }
        else if (connectionData.LobbyConnectionType == LobbyConnectionType.JoinIfExists && !ServerData.ActiveLobbies.TryGetValue(connectionData.LobbyId, out lobby))
        {
            connectingEvtData = await connectionEventBus.Fire(new LobbyNotFoundEvent(connectionData.LobbyId)
            {
                ConnectingUser = connectingEvtData.ConnectingUser,
                ConnectionTime = connectingEvtData.ConnectionTime,
                UserDenied = true,
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

        var result = await connectionEventBus.Fire(new UserRegisteredEvent
        {
            ConnectingUser = user,
            ConnectionTime = DateTime.UtcNow
        });
        if (result.UserDenied)
        {
            transportUtility.SendToRemote(user.UserId, ConnectionPacketBuilder.ConnectionResponse(false, result.ResponsePacket), TransportMethod.Reliable);
            transportUtility.KickRemote(user.UserId);
        }

        ServerData.AddConnectingUser(result);
    }

    private async Task RemoveUser(UserData user)
    {
        ServerData.ConnectedUsers.Remove(user.UserId);

        if (ServerData.ActiveLobbies.TryGetValue(user.LobbyId, out ServerLobby lobby))
        {
            await RemoveUserFromLobby(user, lobby);

            if (lobby.LobbyData.LobbyUsers.Count == 0)
            {
                if (spawnLobbiesOnStart)
                {
                    lobby.UserLeft(user);
                }
                else 
                {
                    await RemoveLobby(lobby);
                }
            }
            else
            {
                lobby.UserLeft(user);
            }
        }

        _ = await connectionEventBus.Fire(new UserRemovedEvent(user));

        Debug.Log($"<color=green><b>CNS</b></color>: Server removed user {user.UserId}.");
    }

    private async Task<ServerLobby> RegisterLobby(int lobbyId)
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
        return lobby;
    }

    private async Task RemoveLobby(ServerLobby lobby)
    {
        ServerData.ActiveLobbies.Remove(lobby.LobbyData.LobbyId);

        _ = await connectionEventBus.Fire(new LobbyRemovedEvent(lobby));

        Destroy(lobby.gameObject);
        if (lobby.LobbyScene.HasValue)
        {
            await SceneManager.UnloadSceneAsync(lobby.LobbyScene.Value);
        }
        Debug.Log($"<color=green><b>CNS</b></color>: Server removed lobby {lobby.LobbyData.LobbyId}.");
    }

    private async Task AddUserToLobby(UserData user, ServerLobby lobby, ConnectionData connectionData)
    {
        user.LobbyId = connectionData.LobbyId;
        lobby.LobbyData.AddUser(user);

        Debug.Log($"<color=green><b>CNS</b></color>: User {user.UserId} joined lobby {lobby.LobbyData.LobbyId}.");
        lobby.UserJoined(user);
    }

    private async Task RemoveUserFromLobby(UserData user, ServerLobby lobby)
    {
        lobby.LobbyData.RemoveUser(user);

        Debug.Log($"<color=green><b>CNS</b></color>: User {user.UserId} left lobby {lobby.LobbyData.LobbyId}.");
    }

    public void RegisterConnectionEventListener(INetEvent listener)
    {
        connectionEventBus.RegisterListener(listener);
    }

    public void UnregisterConnectionEventListener(INetEvent listener)
    {
        connectionEventBus.UnregisterListener(listener);
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

    public void RemoveTransport(TransportType transportType)
    {
        NetTransport transport = transportUtility.Transports.Find(t => t.TransportData.TransportType == transportType);
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

internal enum ConnectionCommandType
{
    CONNECTION_REQUEST,
    CONNECTION_RESPONSE,
#if CNS_SERVER_SINGLE || CNS_SYNC_HOST
    CONNECTION_USER_GUID
#endif
}

internal static class ConnectionPacketBuilder
{
    internal static NetPacket ConnectionResponse(bool accepted, NetPacket dataPkt = null)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ConnectionCommandType.CONNECTION_RESPONSE);
        packet.Write(accepted);
        if (dataPkt != null && dataPkt.Length > 0)
            packet.Write(dataPkt.ByteArray);
        return packet;
    }

#if CNS_SERVER_SINGLE || CNS_SYNC_HOST
    internal static NetPacket ConnectionUserGuid(Guid userGuid)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ConnectionCommandType.CONNECTION_USER_GUID);
        packet.Write(userGuid.ToString());
        return packet;
    }
#endif
}

public class UserRegisteredEvent : ConnectionEvent { }

public class UserRemovedEvent : ConnectionEvent { }

public class ConnectionDataReceivedEvent : ConnectionEvent
{
    public ConnectionData ConnectionData { get; private set; }

    internal ConnectionDataReceivedEvent(ConnectionData data)
    {
        ConnectionData = data;
    }
}

public class LobbyNotFoundEvent : ConnectionEvent
{
    public int LobbyId { get; private set; }

    internal LobbyNotFoundEvent(int lobbyId)
    {
        LobbyId = lobbyId;
    }
}

public class LobbyRegisteredEvent : ConnectionEvent
{
    public ServerLobby Lobby { get; private set; }

    internal LobbyRegisteredEvent(ServerLobby lobby)
    {
        Lobby = lobby;
    }
}

public class LobbyRemovedEvent : ConnectionEvent
{
    public ServerLobby Lobby { get; private set; }

    internal LobbyRemovedEvent(ServerLobby lobby)
    {
        Lobby = lobby;
    }
}
