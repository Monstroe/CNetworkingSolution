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
        public string? ServerToken { get; set; }
#nullable disable
    }
#endif

    public delegate void NewUserCreatedEventHandler(Guid userId);
    public event NewUserCreatedEventHandler OnNewUserCreated;

    public delegate void CurrentUserUpdatedEventHandler(UserSettings userSettings);
    public event CurrentUserUpdatedEventHandler OnCurrentUserUpdated;

#nullable enable
    public delegate void LobbyCreateRequestedEventHandler(int lobbyId, LobbySettings lobbySettings, ServerSettings? serverSettings, string? serverToken);
#nullable disable
    public event LobbyCreateRequestedEventHandler OnLobbyCreateRequested;

    public delegate void CurrentLobbyUpdatedEventHandler(LobbySettings lobbySettings);
    public event CurrentLobbyUpdatedEventHandler OnCurrentLobbyUpdated;

#nullable enable
    public delegate void LobbyJoinRequestedEventHandler(int lobbyId, LobbySettings lobbySettings, ServerSettings? serverSettings, string? serverToken);
#nullable disable
    public event LobbyJoinRequestedEventHandler OnLobbyJoinRequested;

    public delegate void OnLobbyConnectionEstablishedEventHandler(int lobbyId);
    public event OnLobbyConnectionEstablishedEventHandler OnLobbyConnectionEstablished;

    public static ClientManager Instance { get; private set; }

    public ulong ClientTick { get; private set; } = 0;
    public UserData CurrentUser { get; private set; }
    public ClientLobby CurrentLobby { get; private set; }
    public bool IsConnected { get; private set; } = false;

    [SerializeField] private NetTransport transport;

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
    public string LobbyApiUrl
    {
        get => lobbyApiUrl;
        set => lobbyApiUrl = value;
    }
    [Tooltip("The URL of the lobby API. PLEASE DON'T PUT A SLASH AT THE END.")]
    [SerializeField] private string lobbyApiUrl = "http://localhost:5107/api";
#endif

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
    private string webToken;
    private string serverToken;
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
        if (transport)
        {
            transport.OnNetworkConnected += HandleNetworkConnected;
            transport.OnNetworkDisconnected += HandleNetworkDisconnected;
            transport.OnNetworkReceived += HandleNetworkReceived;
        }
    }

    void FixedUpdate()
    {
        ClientTick++;
    }

    private void HandleNetworkConnected(NetTransport transport, ConnectedArgs args)
    {
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            CurrentLobby.SendToServer(PacketBuilder.ConnectionRequest(serverToken), TransportMethod.Reliable);
        }
        else
        {
            CurrentLobby.SendToServer(PacketBuilder.ConnectionRequest(new ConnectionData
            {
                LobbyId = CurrentLobby.LobbyData.LobbyId,
                UserGuid = CurrentUser.GlobalGuid,
                UserSettings = CurrentUser.Settings,
                LobbySettings = CurrentLobby.LobbyData.Settings
            }), TransportMethod.Reliable);
        }
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH || CNS_HOST_AUTH
        CurrentLobby.SendToServer(PacketBuilder.ConnectionRequest(new ConnectionData
        {
            LobbyId = CurrentLobby.LobbyData.LobbyId,
            UserGuid = CurrentUser.GlobalGuid,
            UserSettings = CurrentUser.Settings,
            LobbySettings = CurrentLobby.LobbyData.Settings
        }), TransportMethod.Reliable);
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
                    IsConnected = true;
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

    public void CreateNewUser(UserSettings userSettings = null, bool invokeEvent = true)
    {
        if (GameResources.Instance.GameMode == GameMode.SINGLEPLAYER)
        {
            CreateUser(Guid.NewGuid(), userSettings ?? GameResources.Instance.DefaultUserSettings, invokeEvent);
            return;
        }

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
        StartCoroutine(CreateUserCoroutine(userSettings, invokeEvent));
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH
        CreateUser(Guid.NewGuid(), userSettings ?? GameResources.Instance.DefaultUserSettings, invokeEvent);
#endif
    }

    private void CreateUser(Guid userGuid, UserSettings userSettings, bool invokeEvent)
    {
        UserData userData = new UserData
        {
            GlobalGuid = userGuid,
            Settings = userSettings
        };
        CurrentUser = userData;
        if (invokeEvent)
        {
            OnNewUserCreated?.Invoke(CurrentUser.GlobalGuid);
        }
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
                    OnNewUserCreated?.Invoke(createdUserData.GlobalGuid);
                }
            }
            else
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: Failed to create user: {www.error}");
            }
        }
    }
#endif

    public void UpdateCurrentUser(UserSettings userSettings, bool invokeEvent = true, bool sync = true)
    {
        if (!sync)
        {
            UpdateUser(userSettings, invokeEvent);
            return;
        }

        if (IsConnected)
        {
            CurrentLobby.SendToServer(PacketBuilder.LobbyUserSettings(CurrentUser, userSettings), TransportMethod.Reliable);
        }
        else if (GameResources.Instance.GameMode == GameMode.SINGLEPLAYER)
        {
            UpdateUser(userSettings, invokeEvent);
            return;
        }
        else
        {
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
            StartCoroutine(UpdateUserCoroutine(userSettings, invokeEvent));
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH
            UpdateUser(userSettings, invokeEvent);
#endif
        }

#if CNS_HOST_AUTH
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            StartCoroutine(UpdateUserCoroutine(userSettings, !IsConnected && invokeEvent));
        }
#endif
    }

    private void UpdateUser(UserSettings userSettings, bool invokeEvent)
    {
        CurrentUser.Settings = userSettings;
        if (invokeEvent)
        {
            OnCurrentUserUpdated?.Invoke(userSettings);
        }
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
                    OnCurrentUserUpdated?.Invoke(userSettings);
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


    public void CreateNewLobby(LobbySettings lobbySettings = null, bool invokeEvent = true)
    {
        if (GameResources.Instance.GameMode == GameMode.SINGLEPLAYER)
        {
            CreateLobby(GameResources.Instance.DefaultLobbyId, lobbySettings ?? GameResources.Instance.DefaultLobbySettings, invokeEvent);
            return;
        }

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
        StartCoroutine(CreateLobbyCoroutine(lobbySettings, invokeEvent));
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH
        CreateLobby(GameResources.Instance.DefaultLobbyId, lobbySettings ?? GameResources.Instance.DefaultLobbySettings, invokeEvent);
#endif
    }

    private void CreateLobby(int lobbyId, LobbySettings lobbySettings, bool invokeEvent)
    {
        CurrentLobby.Init(lobbyId, transport);
        CurrentLobby.LobbyData.Settings = lobbySettings ?? GameResources.Instance.DefaultLobbySettings;
        if (invokeEvent)
        {
            OnLobbyCreateRequested?.Invoke(lobbyId, CurrentLobby.LobbyData.Settings, null, null);
        }
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
                serverToken = lobbyResponse.ServerToken;
                Debug.Log($"<color=green><b>CNS</b></color>: Created lobby with Id {createdLobbyData.LobbyId}.");

                if (invokeEvent)
                {
                    OnLobbyCreateRequested?.Invoke(createdLobbyData.LobbyId, CurrentLobby.LobbyData.Settings, lobbyResponse.ServerSettings, lobbyResponse.ServerToken);
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

    public void UpdateCurrentLobby(LobbySettings lobbySettings, bool invokeEvent = true, bool sync = true)
    {
        if (!sync)
        {
            UpdateLobby(lobbySettings, invokeEvent);
            return;
        }

        if (IsConnected)
        {
            CurrentLobby.SendToServer(PacketBuilder.LobbySettings(lobbySettings), TransportMethod.Reliable);
        }
        else if (GameResources.Instance.GameMode == GameMode.SINGLEPLAYER)
        {
            UpdateLobby(lobbySettings, invokeEvent);
            return;
        }
        else
        {
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
            StartCoroutine(UpdateLobbyCoroutine(lobbySettings, invokeEvent));
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH
            UpdateLobby(lobbySettings, invokeEvent);
#endif
        }

#if CNS_HOST_AUTH
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            StartCoroutine(UpdateLobbyCoroutine(lobbySettings, !IsConnected && invokeEvent));
        }
#endif
    }

    private void UpdateLobby(LobbySettings lobbySettings, bool invokeEvent)
    {
        CurrentLobby.LobbyData.Settings = lobbySettings;
        if (invokeEvent)
        {
            OnCurrentLobbyUpdated?.Invoke(lobbySettings);
        }
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
                    OnCurrentLobbyUpdated?.Invoke(lobbySettings);
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

    public void JoinExistingLobby(int lobbyId, bool invokeEvent = true)
    {
        if (GameResources.Instance.GameMode == GameMode.SINGLEPLAYER)
        {
            JoinLobby(lobbyId, GameResources.Instance.DefaultLobbySettings, invokeEvent);
            return;
        }

#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH || CNS_HOST_AUTH
        StartCoroutine(JoinLobbyCoroutine(lobbyId, invokeEvent));
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH
        JoinLobby(lobbyId, GameResources.Instance.DefaultLobbySettings, invokeEvent);
#endif
    }

    private void JoinLobby(int lobbyId, LobbySettings lobbySettings, bool invokeEvent)
    {
        CurrentLobby.Init(lobbyId, transport);
        CurrentLobby.LobbyData.Settings = lobbySettings;
        if (invokeEvent)
        {
            OnLobbyJoinRequested?.Invoke(lobbyId, CurrentLobby.LobbyData.Settings, null, null);
        }
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
                serverToken = lobbyResponse.ServerToken;
                Debug.Log($"<color=green><b>CNS</b></color>: Joined lobby with Id {joinedLobbyData.LobbyId}.");

                if (invokeEvent)
                {
                    OnLobbyJoinRequested?.Invoke(joinedLobbyData.LobbyId, lobbyResponse.LobbySettings, lobbyResponse.ServerSettings, lobbyResponse.ServerToken);
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

    public void SetClientTick(ulong tick)
    {
        ClientTick = tick;
    }

    public void SetCurrentUserData(UserData userData)
    {
        CurrentUser = userData;
    }

    public void SetTransport(TransportType transportType)
    {
        ResetTransport();

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
#if CNS_TRANSPORT_STEAMWORKS && CNS_HOST_AUTH
            case TransportType.SteamWorks:
                transport = gameObject.AddComponent<SteamworksTransport>();
                break;
#endif
        }

        transport.OnNetworkConnected += HandleNetworkConnected;
        transport.OnNetworkDisconnected += HandleNetworkDisconnected;
        transport.OnNetworkReceived += HandleNetworkReceived;
    }

    public void ResetTransport()
    {
        if (transport != null)
        {
            transport.OnNetworkConnected -= HandleNetworkConnected;
            transport.OnNetworkDisconnected -= HandleNetworkDisconnected;
            transport.OnNetworkReceived -= HandleNetworkReceived;
            Destroy(transport);
        }
        transport = null;
    }
}
