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

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
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
    private ServerDatabaseHandler db;
    private ServerTokenVerifier tokenVerifier;
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
            transport.Initialize();
        }
    }

    void Start()
    {
        foreach (NetTransport transport in transports)
        {
            transport.OnNetworkConnected += HandleNetworkConnected;
            transport.OnNetworkDisconnected += HandleNetworkDisconnected;
            transport.OnNetworkReceived += HandleNetworkReceived;
        }

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        OnPublicIpAddressFetched += async (ip) =>
        {
            ServerData.Settings.GameServerId = Guid.NewGuid();
            ServerData.Settings.GameServerKey = GenerateSecretKey();
            ServerData.Settings.GameServerAddress = ip;

            db = new ServerDatabaseHandler();
            await db.Connect(dbConnectionString, ServerData.Settings.GameServerId);
            db.StartHeartbeat(secondsBetweenHeartbeats);

            tokenVerifier = new ServerTokenVerifier(ServerData.Settings.GameServerKey);
            tokenVerifier.StartUnverifiedUserCleanup(maxSecondsBeforeUnverifiedUserRemoval);
            tokenVerifier.StartTokenCleanup(tokenValidityDurationMinutes);
        };

        StartCoroutine(GetPublicIpAddress());
#endif

        foreach (NetTransport transport in transports)
        {
            transport.StartServer();
        }
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
#if !UNITY_EDITOR
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
                    ConnectionData connectionData;
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
                    connectionData = tokenVerifier.VerifyToken(packet.ReadString());
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH || CNS_HOST_AUTH
                    connectionData = new ConnectionData().Deserialize(ref packet);
#else
                    connectionData = null;
#endif

#if CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH
                    connectionData.LobbyId = GameResources.Instance.DefaultLobbyId;
                    connectionData.LobbySettings = GameResources.Instance.DefaultLobbySettings;
#endif
                    if (connectionData != null)
                    {
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
                        tokenVerifier.RemoveUnverifiedUser(remoteUser);
#endif
                        ServerLobby lobby = null;
                        if (ServerData.ActiveLobbies.ContainsKey(connectionData.LobbyId))
                        {
                            lobby = ServerData.ActiveLobbies[connectionData.LobbyId];
                        }
                        else
                        {
                            lobby = await RegisterLobby(connectionData);
                        }
                        lobby.SendToUser(remoteUser, PacketBuilder.ConnectionResponse(true), TransportMethod.Reliable);

                        if (lobby != null && lobby.LobbyData.UserCount < lobby.LobbyData.Settings.MaxUsers)
                        {
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
#if !UNITY_EDITOR
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
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        await db.Close();
#endif
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
            UserId = userId
        };
        ServerData.ConnectedUsers[user.UserId] = user;
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        await db.SaveUserMetadataAsync(user);
        tokenVerifier.AddUnverifiedUser(user);
#endif
        return user;
    }

    private async Task RemoveUser(UserData user)
    {
        ServerData.ConnectedUsers.Remove(user.UserId);
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        await db.DeleteUserAsync(user.GlobalGuid);
        await db.RemoveUserFromGameServerAsync(user.GlobalGuid);
#endif
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
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        tokenVerifier.RemoveUnverifiedUser(user);
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
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        await db.SaveLobbyMetadataAsync(lobby.LobbyData);
        await db.RemoveLobbyFromLimbo(lobby.LobbyData.LobbyId);
        await db.AddLobbyToGameServerAsync(lobby.LobbyData.LobbyId);
#endif
        return lobby;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task RemoveLobby(ServerLobby lobby)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        ServerData.ActiveLobbies.Remove(lobby.LobbyData.LobbyId);
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        await db.RemoveLobbyFromGameServerAsync(lobby.LobbyData.LobbyId);
        await db.DeleteLobbyAsync(lobby.LobbyData.LobbyId);
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
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        await db.SaveUserMetadataAsync(user);
        await db.AddUserToGameServerAsync(user.GlobalGuid);
        await db.AddUserToLobbyAsync(data.LobbyId, user.GlobalGuid);
#endif
        lobby.UserJoined(user);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task RemoveUserFromLobby(UserData user, ServerLobby lobby)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        lobby.LobbyData.LobbyUsers.Remove(user);
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        await db.RemoveUserFromLobbyAsync(user.LobbyId, user.GlobalGuid);
#endif
    }

    public void RegisterTransport(NetTransport transport)
    {
        transports.Add(transport);
        transport.OnNetworkConnected += HandleNetworkConnected;
        transport.OnNetworkDisconnected += HandleNetworkDisconnected;
        transport.OnNetworkReceived += HandleNetworkReceived;
    }

    public (uint, int) GetUserIdAndTransportIndex(UserData user)
    {
        return ((uint)user.UserId, (int)(user.UserId >> 32));
    }

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
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
