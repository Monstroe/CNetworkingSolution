using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance { get; private set; }

    public ulong ServerTick { get; private set; } = 0;
    public ServerData ServerData { get; private set; } = new ServerData();

    [SerializeField] private List<NetTransport> transports;

#if CNS_SYNC_SERVER_MULTIPLE
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
    [SerializeField] private int tokenValidityDurationMinutes = 2;
    public ServerDatabaseHandler Database { get; private set; }
    public ServerTokenVerifier TokenVerifier { get; private set; }
#endif

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
        }

        Debug.Log("<color=green><b>CNS</b></color>: Initializing ServerManager.");

        foreach (NetTransport transport in transports)
        {
            transport.Initialize(NetDeviceType.Server);
            transport.StartDevice();
            transport.OnNetworkConnected += HandleNetworkConnected;
            transport.OnNetworkDisconnected += HandleNetworkDisconnected;
            transport.OnNetworkReceived += HandleNetworkReceived;
        }

        Debug.Log("<color=green><b>CNS</b></color>: ServerManager initialized.");

#if CNS_SYNC_SERVER_MULTIPLE
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            OnPublicIpAddressFetched += async (ip) =>
            {
                await InitServer(ip);
            };
            StartCoroutine(GetPublicIpAddress());
        }
#endif
    }

    public void BroadcastMessage(NetPacket packet, TransportMethod method)
    {
        foreach (NetTransport transport in transports)
        {
            transport.SendToAll(packet, method);
        }
    }

    public void KickUser(UserData user)
    {
        var (userId, transportIndex) = GetUserIdAndTransportIndex(user);
        transports[transportIndex].DisconnectRemote(userId);
    }

    public void Shutdown()
    {
        foreach (NetTransport transport in transports)
        {
            transport.Shutdown();
        }
    }

    private async void HandleNetworkConnected(NetTransport transport, ConnectedArgs args)
    {
        try
        {
            if (!ServerData.ConnectedUsers.ContainsKey(args.RemoteId))
            {
                await RegisterUser(transport, args.RemoteId);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User with ID {args.RemoteId} attempted to connect again.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error handling connection for user {args.RemoteId}: {ex.Message}");
        }
    }

    private async void HandleNetworkDisconnected(NetTransport transport, DisconnectedArgs args)
    {
        try
        {
            if (ServerData.ConnectedUsers.ContainsKey(args.RemoteId))
            {
                UserData userData = ServerData.ConnectedUsers[args.RemoteId];
                await RemoveUser(userData);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User with ID {args.RemoteId} was not connected.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error handling disconnection for user {args.RemoteId}: {ex.Message}");
        }
    }

    private async void HandleNetworkReceived(NetTransport transport, ReceivedArgs args)
    {
        if (ServerData.ConnectedUsers.TryGetValue(args.RemoteId, out UserData remoteUser))
        {
#if UNITY_EDITOR
            try
            {
#endif
            if (remoteUser.InLobby && ServerData.ActiveLobbies.ContainsKey(remoteUser.LobbyId))
            {
                ServerLobby lobby = ServerData.ActiveLobbies[remoteUser.LobbyId];
                lobby.ReceiveData(remoteUser, args.Packet, args.TransportMethod);
            }
            else
            {
                NetPacket packet = args.Packet;
                ServiceType serviceType = (ServiceType)packet.ReadByte();
                CommandType commandType = (CommandType)packet.ReadByte();
                if (serviceType == ServiceType.CONNECTION && commandType == CommandType.CONNECTION_REQUEST)
                {
                    ConnectionData connectionData = GetConnectionData(packet);
                    if (connectionData != null)
                    {
                        ServerLobby lobby = null;
                        if (ServerData.ActiveLobbies.ContainsKey(connectionData.LobbyId))
                        {
                            lobby = ServerData.ActiveLobbies[connectionData.LobbyId];
                        }
                        else
                        {
                            lobby = await RegisterLobby(connectionData);
                            }

                            if (lobby != null && lobby.LobbyData.UserCount < lobby.LobbyData.Settings.MaxUsers)
                            {
                                lobby.SendToUser(remoteUser, PacketBuilder.ConnectionResponse(true), TransportMethod.Reliable);
                                await AddUserToLobby(remoteUser, lobby, connectionData);
                            }
                        else
                        {
                            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Lobby {connectionData.LobbyId} is full. User {args.RemoteId} cannot join.");
                            KickUser(remoteUser);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Invalid connection data received from user {args.RemoteId}.");
                        KickUser(remoteUser);
                    }
                }
                else
                {
                    Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User {args.RemoteId} is not in any active lobby.");
                    KickUser(remoteUser);
                }
            }
#if UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error when processing received data from user {args.RemoteId}: {ex.Message}");
                KickUser(remoteUser);
            }
#endif
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received data from unknown user ID {args.RemoteId}.");
            KickUser(new UserData { UserId = args.RemoteId });
        }
    }

    void FixedUpdate()
    {
        foreach (ServerLobby serverLobby in ServerData.ActiveLobbies.Values)
        {
            serverLobby.Tick();
        }

        ServerTick++;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    async void OnDestroy()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
#if CNS_SYNC_SERVER_MULTIPLE
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            await Database.Close();
        }
#endif
    }

    private ConnectionData GetConnectionData(NetPacket packet)
    {
        if (GameResources.Instance.GameMode == GameMode.SINGLEPLAYER)
        {
            return new ConnectionData().Deserialize(packet);
        }

        ConnectionData connectionData = null;
#if CNS_SYNC_SERVER_MULTIPLE
        connectionData = TokenVerifier.VerifyToken(packet.ReadString());
#elif CNS_SYNC_SERVER_SINGLE || CNS_SYNC_HOST
        connectionData = new ConnectionData().Deserialize(packet);
#else
        connectionData = null;
#endif

#if CNS_SYNC_LOBBY_SINGLE
        if (connectionData.LobbyId == GameResources.Instance.DefaultLobbyId)
        {
            connectionData.LobbySettings = GameResources.Instance.DefaultLobbySettings;
        }
        else
        {
            connectionData = null;
        }
#endif
        return connectionData;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task<UserData> RegisterUser(NetTransport transport, uint remoteId)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        int transportIndex = -1;
        for (int i = 0; i < transports.Count; i++)
        {
            if (transports[i] == transport)
            {
                transportIndex = i;
                break;
            }
        }
        ulong userId = (ulong)transportIndex << 32 | remoteId;

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

#if CNS_SYNC_SERVER_MULTIPLE
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            await Database.AddUserToServerLimboAsync(user.UserId, maxSecondsBeforeUnverifiedUserRemoval);
            TokenVerifier.AddUnverifiedUser(user);
        }
#endif
        return user;
    }

    private async Task RemoveUser(UserData user)
    {
        ServerData.ConnectedUsers.Remove(user.UserId);
        if (ServerData.ActiveLobbies.ContainsKey(user.LobbyId))
        {
            ServerLobby lobby = ServerData.ActiveLobbies[user.LobbyId];
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

#if CNS_SYNC_SERVER_MULTIPLE
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            await Database.DeleteUserAsync(user.GlobalGuid);
            await Database.RemoveUserFromServerAsync(user.GlobalGuid);
            await Database.RemoveUserFromServerLimboAsync(user.UserId);
            TokenVerifier.RemoveUnverifiedUser(user);
        }
#endif
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task<ServerLobby> RegisterLobby(ConnectionData data)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        ServerLobby lobby = new GameObject($"Lobby_{data.LobbyId}").AddComponent<ServerLobby>();
        lobby.Init(data.LobbyId, transports);
        lobby.LobbyData.Settings = data.LobbySettings;
        lobby.transform.SetParent(gameObject.transform);
        ServerData.ActiveLobbies.Add(lobby.LobbyData.LobbyId, lobby);

#if CNS_SYNC_SERVER_MULTIPLE
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            await Database.SaveLobbyMetadataAsync(lobby.LobbyData);
            await Database.RemoveLobbyFromLimbo(lobby.LobbyData.LobbyId);
            await Database.AddLobbyToServerAsync(lobby.LobbyData.LobbyId);
        }
#endif
        return lobby;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task RemoveLobby(ServerLobby lobby)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        ServerData.ActiveLobbies.Remove(lobby.LobbyData.LobbyId);

#if CNS_SYNC_SERVER_MULTIPLE
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            await Database.RemoveLobbyFromServerAsync(lobby.LobbyData.LobbyId);
            await Database.DeleteLobbyAsync(lobby.LobbyData.LobbyId);
        }
#endif
        Destroy(lobby.gameObject);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task AddUserToLobby(UserData user, ServerLobby lobby, ConnectionData data)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        user.LobbyId = data.LobbyId;
        user.GlobalGuid = data.UserGuid;
        user.Settings = data.UserSettings;
        lobby.LobbyData.LobbyUsers.Add(user);

#if CNS_SYNC_SERVER_MULTIPLE
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            TokenVerifier.RemoveUnverifiedUser(user);
            await Database.SaveUserMetadataAsync(user);
            await Database.RemoveUserFromServerLimboAsync(user.UserId);
            await Database.AddUserToServerAsync(user.GlobalGuid);
            await Database.AddUserToLobbyAsync(data.LobbyId, user.GlobalGuid);
        }
#endif
        lobby.UserJoined(user);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task RemoveUserFromLobby(UserData user, ServerLobby lobby)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        lobby.LobbyData.LobbyUsers.Remove(user);

#if CNS_SYNC_SERVER_MULTIPLE
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            await Database.RemoveUserFromLobbyAsync(user.LobbyId, user.GlobalGuid);
        }
#endif
    }

    public void AddTransport(TransportType transportType)
    {
        NetTransport transport = null;

        switch (transportType)
        {
#if CNS_TRANSPORT_LOCAL
            case TransportType.Local:
                transport = gameObject.AddComponent<LocalTransport>();
                break;
#endif
#if CNS_TRANSPORT_LITENETLIB
            case TransportType.LiteNetLib:
                transport = gameObject.AddComponent<LiteNetLibTransport>();
                break;
#endif
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
            case TransportType.SteamWorks:
                transport = gameObject.AddComponent<SteamRelayTransport>();
                break;
#endif
        }

        transports.Add(transport);
        transport.Initialize(NetDeviceType.Server);
        transport.StartDevice();
        transport.OnNetworkConnected += HandleNetworkConnected;
        transport.OnNetworkDisconnected += HandleNetworkDisconnected;
        transport.OnNetworkReceived += HandleNetworkReceived;
    }

    public void ClearTransports()
    {
        foreach (NetTransport transport in transports)
        {
            transport.OnNetworkConnected -= HandleNetworkConnected;
            transport.OnNetworkDisconnected -= HandleNetworkDisconnected;
            transport.OnNetworkReceived -= HandleNetworkReceived;
            Destroy(transport);
        }
        transports.Clear();
    }

    public (uint, int) GetUserIdAndTransportIndex(UserData user)
    {
        return ((uint)user.UserId, (int)(user.UserId >> 32));
    }

#if CNS_SYNC_SERVER_MULTIPLE
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
                Debug.LogError($"<color=red><b>CNS</b></color>: Failed to fetch lobby data: {www.error}");
            }
        }
    }

    private async Task InitServer(string address)
    {
        ServerData.Settings.ServerId = Guid.NewGuid();
        ServerData.Settings.ServerKey = GenerateSecretKey();
        ServerData.Settings.ServerAddress = address;

        await InitDatabase();
        InitTokenVerifier();
    }

    private async Task InitDatabase()
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
        TokenVerifier.StartTokenCleanup(tokenValidityDurationMinutes);
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
