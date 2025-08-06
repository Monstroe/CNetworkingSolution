using System;
using System.Collections;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class ServerManager : MonoBehaviour
{
    public delegate void PublicIpAddressFetchedHandler(string ipAddress);
    public event PublicIpAddressFetchedHandler OnPublicIpAddressFetched;

    class PublicIpAddress
    {
        public string Ip { get; set; }
    }

    public static ServerManager Instance { get; private set; }

    public int ServerTick { get; private set; }
    public GameServerData GameServerData { get; private set; } = new GameServerData();

    [SerializeField] private Transform lobbyParent;
    [SerializeField] private string publicIpApiUrl = "https://api.ipify.org/?format=json";

    private NetTransport transport;

#if CNS_DATABASE_ACCESS
    [Header("Database Settings")]
    [SerializeField] private string connectionString = "localhost:6379";
    [SerializeField] private int secondsBetweenHeartbeats = 30;
    private ServerDatabaseHandler db;
#endif

#if CNS_TOKEN_VERIFIER
    [Header("Token Settings")]
    [SerializeField] private int maxSecondsBeforeUnverifiedUserRemoval = 15;
    [SerializeField] private int tokenValidityDurationMinutes = 2;
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
    }

    void Start()
    {
        NetworkManager.Instance.OnNetworkConnected += HandleNetworkConnected;
        NetworkManager.Instance.OnNetworkDisconnected += HandleNetworkDisconnected;
        NetworkManager.Instance.OnNetworkReceived += HandleNetworkReceived;

        OnPublicIpAddressFetched += async (ip) =>
        {
            GameServerData.GameServerId = Guid.NewGuid();
            GameServerData.GameServerKey = GenerateSecretKey();
            GameServerData.GameServerAddress = ip;

#if CNS_DATABASE_ACCESS
            db = new ServerDatabaseHandler();
            await db.Connect(connectionString, GameServerData.GameServerId);
            db.StartHeartbeat(secondsBetweenHeartbeats);
#endif

#if CNS_TOKEN_VERIFIER
            tokenVerifier = new ServerTokenVerifier(GameServerData.GameServerKey);
            tokenVerifier.StartUnverifiedUserCleanup(maxSecondsBeforeUnverifiedUserRemoval);
            tokenVerifier.StartTokenCleanup(tokenValidityDurationMinutes);
#endif
        };

        StartCoroutine(GetPublicIpAddress());
    }

    public void Init(NetTransport transport)
    {
        this.transport = transport;
        transport.Initialize();
    }

    public void BroadcastMessage(NetPacket packet, TransportMethod method)
    {
        transport.SendToAll(packet, method);
    }

    public void KickUser(UserData user)
    {
        transport.DisconnectRemote(user.UserId);
    }

    public void Shutdown()
    {
        transport.Shutdown();
    }

    private async void HandleNetworkConnected(ConnectedArgs args)
    {
        try
        {
            if (!GameServerData.ConnectedUsers.ContainsKey(args.RemoteId))
            {
                await RegisterUser(args.RemoteId);
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

    private async void HandleNetworkDisconnected(DisconnectedArgs args)
    {
        try
        {
            if (GameServerData.ConnectedUsers.ContainsKey(args.RemoteId))
            {
                UserData userData = GameServerData.ConnectedUsers[args.RemoteId];
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

    private async void HandleNetworkReceived(ReceivedArgs args)
    {
        try
        {
            if (GameServerData.ConnectedUsers.ContainsKey(args.RemoteId))
            {
                UserData remoteUser = GameServerData.ConnectedUsers[args.RemoteId];
                if (remoteUser.InLobby && GameServerData.ActiveLobbies.ContainsKey(remoteUser.LobbyId))
                {
                    ServerLobby lobby = GameServerData.ActiveLobbies[remoteUser.LobbyId];
                    lobby.ReceiveData(remoteUser, args.Packet, args.TransportMethod);
                }
                else
                {
                    NetPacket packet = args.Packet;
                    ServiceType serviceType = (ServiceType)packet.ReadByte();
                    if (serviceType == ServiceType.CONNECTION)
                    {
                        ServerConnectionData connectionData;
#if CNS_TOKEN_VERIFIER
                        connectionData = tokenVerifier.VerifyToken(packet.ReadString());
#else
                        connectionData = new ServerConnectionData
                        {
                            LobbyId = packet.ReadInt(),
                            UserGuid = Guid.Parse(packet.ReadString()),
                            UserSettings = new UserSettings
                            {
                                UserName = packet.ReadString()
                            },
                            LobbySettings = new LobbySettings
                            {
                                InternalCode = packet.ReadULong(),
                                MaxUsers = packet.ReadInt(),
                                LobbyVisibility = Enum.Parse<LobbyVisibility>(packet.ReadString()),
                                LobbyName = packet.ReadString()
                            }
                        };
#endif
                        if (connectionData != null)
                        {
#if CNS_TOKEN_VERIFIER
                            tokenVerifier.RemoveUnverifiedUser(remoteUser);
#endif
                            ServerLobby lobby = null;
                            if (GameServerData.ActiveLobbies.ContainsKey(connectionData.LobbyId))
                            {
                                lobby = GameServerData.ActiveLobbies[connectionData.LobbyId];
                            }
                            else
                            {
#if CNS_MULTI_LOBBY
                                lobby = await RegisterLobby(connectionData);
#endif
#if CNS_SINGLE_LOBBY
                                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Lobby {connectionData.LobbyId} does not exist. Disconnecting user {args.RemoteId}.");
                                KickUser(remoteUser);
                                return;
#endif
                            }

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
                            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Invalid token received from user {args.RemoteId}.");
                            KickUser(remoteUser);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User {args.RemoteId} is not in any active lobby.");
                        KickUser(remoteUser);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received data from unknown user ID {args.RemoteId}.");
                KickUser(new UserData { UserId = args.RemoteId });
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error when processing received data from user {args.RemoteId}: {ex.Message}");
        }
    }

    void FixedUpdate()
    {
        foreach (ServerLobby serverLobby in GameServerData.ActiveLobbies.Values)
        {
            serverLobby.Tick();
        }

        ServerTick++;
    }

    async void OnDestroy()
    {
#if CNS_DATABASE_ACCESS
        await db.Close();
#endif
    }

    private async Task<UserData> RegisterUser(uint remoteId)
    {
        UserData user = new UserData
        {
            UserId = remoteId
        };
        GameServerData.ConnectedUsers[user.UserId] = user;
#if CNS_DATABASE_ACCESS
        await db.SaveUserMetadataAsync(user);
#endif
#if CNS_TOKEN_VERIFIER
        tokenVerifier.AddUnverifiedUser(user);
#endif
        return user;
    }

    private async Task RemoveUser(UserData user)
    {
        GameServerData.ConnectedUsers.Remove(user.UserId);
#if CNS_DATABASE_ACCESS
        await db.DeleteUserAsync(user.GlobalGuid);
        await db.RemoveUserFromGameServerAsync(user.GlobalGuid);
#endif
        if (GameServerData.ActiveLobbies.ContainsKey(user.LobbyId))
        {
            ServerLobby lobby = GameServerData.ActiveLobbies[user.LobbyId];
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
#if CNS_TOKEN_VERIFIER
        tokenVerifier.RemoveUnverifiedUser(user);
#endif
    }

    private async Task<ServerLobby> RegisterLobby(ServerConnectionData data)
    {
        ServerLobby lobby = new GameObject().AddComponent<ServerLobby>();
        lobby.Init(data.LobbyId, transport);
        lobby.LobbyData.Settings = data.LobbySettings;
        lobby.transform.SetParent(lobbyParent);
        GameServerData.ActiveLobbies.Add(lobby.LobbyData.LobbyId, lobby);
#if CNS_DATABASE_ACCESS
        await db.SaveLobbyMetadataAsync(lobby.LobbyData);
        await db.RemoveLobbyFromLimbo(lobby.LobbyData.LobbyId);
        await db.AddLobbyToGameServerAsync(lobby.LobbyData.LobbyId);
#endif

        return lobby;
    }

    private async Task RemoveLobby(ServerLobby lobby)
    {
        GameServerData.ActiveLobbies.Remove(lobby.LobbyData.LobbyId);
#if CNS_DATABASE_ACCESS
        await db.RemoveLobbyFromGameServerAsync(lobby.LobbyData.LobbyId);
        await db.DeleteLobbyAsync(lobby.LobbyData.LobbyId);
#endif
        Destroy(lobby.gameObject);
    }

    private async Task AddUserToLobby(UserData user, ServerLobby lobby, ServerConnectionData data)
    {
        user.LobbyId = data.LobbyId;
        user.GlobalGuid = data.UserGuid;
        user.Settings = data.UserSettings;
        lobby.LobbyData.LobbyUsers.Add(user);
#if CNS_DATABASE_ACCESS
        await db.SaveUserMetadataAsync(user);
        await db.AddUserToGameServerAsync(user.GlobalGuid);
        await db.AddUserToLobbyAsync(data.LobbyId, user.GlobalGuid);
#endif
        lobby.UserJoined(user);
    }

    private async Task RemoveUserFromLobby(UserData user, ServerLobby lobby)
    {
        lobby.LobbyData.LobbyUsers.Remove(user);
#if CNS_DATABASE_ACCESS
        await db.RemoveUserFromLobbyAsync(user.LobbyId, user.GlobalGuid);
#endif
    }

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
}

public class ServerConnectionData
{
#if CNS_TOKEN_VERIFIER
    public Guid TokenId { get; set; }
#endif
    public int LobbyId { get; set; }
    public Guid UserGuid { get; set; }
    public UserSettings UserSettings { get; set; }
    public LobbySettings LobbySettings { get; set; }
}