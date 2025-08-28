using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(ClientLobby))]
public class ClientManager : MonoBehaviour
{
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
    class UserResponse
    {
        public Guid GlobalGuid { get; set; }
        public UserSettings UserSettings { get; set; }
        public string Token { get; set; }
    }

    class LobbyResponse
    {
        public int LobbyId { get; set; }
        public LobbySettings LobbySettings { get; set; }
#nullable enable
        public ServerSettings? ServerSettings { get; set; }
        public string? GameServerToken { get; set; }
#nullable disable
    }
#endif

    public delegate void UserCreatedEventHandler(Guid userId);
    public event UserCreatedEventHandler OnUserCreated;

    public delegate void UserUpdatedEventHandler(UserSettings userSettings);
    public event UserUpdatedEventHandler OnUserUpdated;

#nullable enable
    public delegate void LobbyCreateRequestedEventHandler(int lobbyId, LobbySettings? lobbySettings, ServerSettings? serverSettings, string? gameServerToken);
#nullable disable
    public event LobbyCreateRequestedEventHandler OnLobbyCreateRequested;

    public delegate void LobbyUpdatedEventHandler(LobbySettings lobbySettings);
    public event LobbyUpdatedEventHandler OnLobbyUpdated;

#nullable enable
    public delegate void LobbyJoinRequestedEventHandler(int lobbyId, LobbySettings? lobbySettings, ServerSettings? serverSettings, string? gameServerToken);
#nullable disable
    public event LobbyJoinRequestedEventHandler OnLobbyJoinRequested;

    public delegate void OnLobbyConnectionEstablishedEventHandler(int lobbyId);
    public event OnLobbyConnectionEstablishedEventHandler OnLobbyConnectionEstablished;

    public static ClientManager Instance { get; private set; }

    public int ClientTick { get; private set; } = 0;
    public UserData CurrentUser { get; private set; }
    public ClientLobby CurrentLobby { get; private set; }
    public bool IsConnected { get; private set; } = false;

    public UserSettings DefaultUserSettings => new UserSettings
    {
        UserName = "DefaultPlayer"
    };

    public LobbySettings DefaultLobbySettings => new LobbySettings
    {
#if CNS_TRANSPORT_STEAMWORKS
        SteamCode = 0,
#endif
        LobbyName = "Default Lobby",
        MaxUsers = 256,
        LobbyVisibility = LobbyVisibility.PRIVATE
    };

    [SerializeField] private NetTransport transport;

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
    public string LobbyApiUrl
    {
        get => lobbyApiUrl;
        set => lobbyApiUrl = value;
    }
    [Tooltip("The URL of the lobby API. PLEASE PUT SLASH AT THE END.")]
    [SerializeField] private string lobbyApiUrl = "http://localhost:8080/api/";
#endif

#if CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH
    public int DefaultLobbyId
    {
        get => defaultLobbyId;
        set => defaultLobbyId = value;
    }
    [Tooltip("The ID of the lobby to connect to on the server.")]
    [SerializeField] private int defaultLobbyId = 0;
#endif

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
    private string webToken;
    private string gameServerToken;
#endif

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Multiple instances of ClientManager detected. Destroying duplicate instance.");
            Destroy(gameObject);
        }

        CurrentLobby = GetComponent<ClientLobby>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transport.OnNetworkConnected += HandleNetworkConnected;
        transport.OnNetworkDisconnected += HandleNetworkDisconnected;
        transport.OnNetworkReceived += HandleNetworkReceived;
    }

    public void InitClientTick(int tick)
    {
        ClientTick = tick;
    }

    void FixedUpdate()
    {
        ClientTick++;
    }

    private void HandleNetworkConnected(NetTransport transport, ConnectedArgs args)
    {
        IsConnected = true;
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        CurrentLobby.SendToServer(PacketBuilder.ConnectionRequest(gameServerToken), TransportMethod.Reliable);
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH || CNS_HOST_AUTH
        ConnectionData connectionData = new ConnectionData
        {
            LobbyId = CurrentLobby.LobbyData.LobbyId,
            UserGuid = CurrentUser.GlobalGuid,
            UserSettings = CurrentUser.Settings,
            LobbySettings = CurrentLobby.LobbyData.Settings
        };
        CurrentLobby.SendToServer(PacketBuilder.ConnectionRequest(connectionData), TransportMethod.Reliable);
#endif
    }

    private void HandleNetworkDisconnected(NetTransport transport, DisconnectedArgs args)
    {
        IsConnected = false;
    }

    private void HandleNetworkReceived(NetTransport transport, ReceivedArgs args)
    {
        if (CurrentUser.InLobby)
        {
            CurrentLobby.ReceiveData(args.Packet, args.TransportMethod);
        }
        else
        {
            NetPacket packet = args.Packet;
            ServiceType serviceType = (ServiceType)packet.ReadByte();
            CommandType commandType = (CommandType)packet.ReadByte();
            if (serviceType == ServiceType.CONNECTION && commandType == CommandType.CONNECTION_RESPONSE)
            {
                bool accepted = packet.ReadBool();
                if (accepted)
                {
                    CurrentUser.LobbyId = CurrentLobby.LobbyData.LobbyId;
                    OnLobbyConnectionEstablished?.Invoke(CurrentLobby.LobbyData.LobbyId);
                }
            }
            else
            {
                Debug.LogWarning($"<color=red><b>CNS</b></color>: Received packet with unknown service type {serviceType} and command type {commandType} while not in a lobby.");
            }
        }
    }

    public void Disconnect()
    {
        transport.Disconnect();
    }

    public void Shutdown()
    {
        transport.Shutdown();
    }

    public void CreateNewUser(UserSettings userSettings = null)
    {
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
        StartCoroutine(CreateUserCoroutine(userSettings, true));
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH
        UserData userData = new UserData
        {
            GlobalGuid = Guid.NewGuid(),
            Settings = userSettings ?? new UserSettings()
        };
        CurrentUser = userData;
        OnUserCreated?.Invoke(userData.GlobalGuid);
#endif
    }

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
    private IEnumerator CreateUserCoroutine(UserSettings userSettings, bool invokeEvent)
    {
        string json = JsonConvert.SerializeObject(userSettings);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest($"{LobbyApiUrl}/user/create", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonBytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var userResponse = JsonConvert.DeserializeObject<UserResponse>(www.downloadHandler.text);
                UserData createdUserData = new UserData
                {
                    GlobalGuid = userResponse.GlobalGuid,
                    Settings = userResponse.UserSettings,
                };
                CurrentUser = createdUserData;
                webToken = userResponse.Token;

                if (invokeEvent)
                {
                    OnUserCreated?.Invoke(createdUserData.GlobalGuid);
                }
            }
            else
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: Failed to create user: {www.error}");
            }
        }
    }
#endif

    public void UpdateCurrentUser(UserSettings userSettings, bool invokeEvent = true)
    {
        if (IsConnected)
        {
            CurrentLobby.SendToServer(PacketBuilder.LobbyUserSettings(CurrentUser, userSettings), TransportMethod.Reliable);
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
            CurrentUser.Settings = userSettings;
            OnUserUpdated?.Invoke(userSettings);
#endif
        }
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        else
        {
            StartCoroutine(UpdateUserCoroutine(userSettings, invokeEvent));
        }
#endif

#if CNS_HOST_AUTH
        StartCoroutine(UpdateUserCoroutine(userSettings, invokeEvent));
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH
        CurrentUser.Settings = userSettings;
        OnUserUpdated?.Invoke(userSettings);
#endif
    }

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
    private IEnumerator UpdateUserCoroutine(UserSettings userSettings, bool invokeEvent)
    {
        string json = JsonConvert.SerializeObject(userSettings);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest($"{LobbyApiUrl}/user/update", "PUT"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonBytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {webToken}");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                CurrentUser.Settings = userSettings;
                if (invokeEvent)
                {
                    OnUserUpdated?.Invoke(userSettings);
                }
            }
            else
            {
                // User expired, create a new user
                if (www.responseCode == 401)
                {
                    yield return CreateUserCoroutine(userSettings, false);
                    yield return UpdateUserCoroutine(userSettings, invokeEvent);
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Failed to update user: {www.error}");
                }
            }
        }
    }
#endif


    public void CreateLobby(LobbySettings lobbySettings = null, bool invokeEvent = true)
    {
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
        StartCoroutine(CreateLobbyCoroutine(lobbySettings, invokeEvent));
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH
        CurrentLobby.Init(defaultLobbyId, transport);
        OnLobbyCreateRequested?.Invoke(defaultLobbyId, lobbySettings ?? DefaultLobbySettings, null, null);
#endif
    }

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
    private IEnumerator CreateLobbyCoroutine(LobbySettings lobbySettings, bool invokeEvent)
    {
        string json = JsonConvert.SerializeObject(lobbySettings);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest($"{LobbyApiUrl}/lobby/create", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonBytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {webToken}");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var lobbyResponse = JsonConvert.DeserializeObject<LobbyResponse>(www.downloadHandler.text);
                LobbyData createdLobbyData = new LobbyData
                {
                    LobbyId = lobbyResponse.LobbyId,
                    Settings = lobbyResponse.LobbySettings
                };
                CurrentLobby.Init(createdLobbyData.LobbyId, transport);
                CurrentLobby.LobbyData.Settings = createdLobbyData.Settings;
                gameServerToken = lobbyResponse.GameServerToken;

                if (invokeEvent)
                {
                    OnLobbyCreateRequested?.Invoke(createdLobbyData.LobbyId, createdLobbyData.LobbySettings, lobbyResponse.ServerSettings, lobbyResponse.GameServerToken);
                }
            }
            else
            {
                // User expired, create a new user
                if (www.responseCode == 401)
                {
                    yield return CreateUserCoroutine(CurrentUser.Settings, false);
                    yield return CreateLobbyCoroutine(lobbySettings, invokeEvent);
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Failed to create lobby: {www.error}");
                }
            }
        }
    }
#endif

    public void UpdateLobby(LobbySettings lobbySettings, bool invokeEvent = true)
    {
        if (IsConnected)
        {
            CurrentLobby.SendToServer(PacketBuilder.LobbySettings(lobbySettings), TransportMethod.Reliable);

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
            CurrentLobby.LobbyData.Settings = lobbySettings;
            OnLobbyUpdated?.Invoke(lobbySettings);
#endif
        }
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        else
        {
            StartCoroutine(UpdateLobbyCoroutine(lobbySettings, invokeEvent));
        }
#endif

#if CNS_HOST_AUTH
        StartCoroutine(UpdateLobbyCoroutine(lobbySettings, invokeEvent));
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH
        CurrentLobby.LobbyData.Settings = lobbySettings;
        OnLobbyUpdated?.Invoke(lobbySettings);
#endif
    }

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
    private IEnumerator UpdateLobbyCoroutine(LobbySettings lobbySettings, bool invokeEvent)
    {
        string json = JsonConvert.SerializeObject(lobbySettings);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest($"{LobbyApiUrl}/lobby/update", "PUT"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonBytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {webToken}");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                CurrentLobby.LobbyData.Settings = lobbySettings;
                if (invokeEvent)
                {
                    OnLobbyUpdated?.Invoke(lobbySettings);
                }
            }
            else
            {
                // User expired, create a new user
                if (www.responseCode == 401)
                {
                    yield return CreateUserCoroutine(CurrentUser.Settings, false);
                    yield return UpdateLobbyCoroutine(lobbySettings, invokeEvent);
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Failed to create lobby: {www.error}");
                }
            }
        }
    }
#endif

    public void JoinLobby(int lobbyId, bool invokeEvent = true)
    {
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
        StartCoroutine(JoinLobbyCoroutine(lobbyId, invokeEvent));
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH
        CurrentLobby.Init(lobbyId, transport);
        OnLobbyJoinRequested?.Invoke(lobbyId, null, null, null);
#endif
    }

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
    private IEnumerator JoinLobbyCoroutine(int lobbyId, bool invokeEvent)
    {
        using (var www = new UnityWebRequest($"{LobbyApiUrl}/lobby/join/{lobbyId}", "GET"))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Authorization", $"Bearer {webToken}");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // TODO: Come back to this
                var lobbyResponse = JsonConvert.DeserializeObject<LobbyResponse>(www.downloadHandler.text);
                LobbyData joinedLobbyData = new LobbyData
                {
                    LobbyId = lobbyResponse.LobbyId,
                    Settings = lobbyResponse.LobbySettings
                };
                CurrentLobby.Init(joinedLobbyData.LobbyId, transport);
                CurrentLobby.LobbyData.Settings = joinedLobbyData.Settings;
                gameServerToken = lobbyResponse.GameServerToken;

                if (invokeEvent)
                {
                    OnLobbyJoinRequested?.Invoke(joinedLobbyData.LobbyId, lobbyResponse.LobbySettings, lobbyResponse.ServerSettings, lobbyResponse.GameServerToken);
                }
            }
            else
            {
                // User expired, create a new user
                if (www.responseCode == 401)
                {
                    yield return CreateUserCoroutine(CurrentUser.Settings, false);
                    yield return JoinLobbyCoroutine(lobbyId, invokeEvent);
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Failed to join lobby: {www.error}");
                }
            }
        }
    }
#endif

    public void RegisterTransport(NetTransport transport)
    {
        this.transport = transport;
        transport.OnNetworkConnected += HandleNetworkConnected;
        transport.OnNetworkDisconnected += HandleNetworkDisconnected;
        transport.OnNetworkReceived += HandleNetworkReceived;
    }
}
