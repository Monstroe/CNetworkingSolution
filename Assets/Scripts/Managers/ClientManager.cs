using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(ClientLobby))]
public class ClientManager : MonoBehaviour
{
#if CNS_SERVER_MULTIPLE
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
    public delegate void LobbyCreateRequestedEventHandler(ServerSettings? serverSettings);
#nullable disable
    public event LobbyCreateRequestedEventHandler OnLobbyCreateRequested;

    public delegate void CurrentLobbyUpdatedEventHandler(LobbySettings lobbySettings);
    public event CurrentLobbyUpdatedEventHandler OnCurrentLobbyUpdated;

#nullable enable
    public delegate void LobbyJoinRequestedEventHandler(int lobbyId, ServerSettings? serverSettings);
#nullable disable
    public event LobbyJoinRequestedEventHandler OnLobbyJoinRequested;

    public delegate void LobbyConnectionAcceptedEventHandler(int lobbyId);
    public event LobbyConnectionAcceptedEventHandler OnLobbyConnectionAccepted;

    public delegate void LobbyConnectionRejectedEventHandler(int lobbyId, LobbyRejectionType errorType);
    public event LobbyConnectionRejectedEventHandler OnLobbyConnectionRejected;

    public delegate void LobbyConnectionLost();
    public event LobbyConnectionLost OnLobbyConnectionLost;

    public static ClientManager Instance { get; private set; }

    public ulong ClientTick { get; private set; } = 0;
    public UserData CurrentUser { get; private set; }
    public ClientLobby CurrentLobby { get; private set; }
    public bool IsConnected { get; private set; } = false;
    public ConnectionData ConnectionData { get; private set; }

#if CNS_SERVER_MULTIPLE
    public string ConnectionToken { get; private set; }
    public ServerSettings CurrentServerSettings { get; set; }

    public string LobbyApiUrl
    {
        get => lobbyApiUrl;
        set => lobbyApiUrl = value;
    }
    [Tooltip("The URL of the lobby API. PLEASE DON'T PUT A SLASH AT THE END.")]
    [SerializeField] private string lobbyApiUrl = "http://localhost:5107/api";

    private string webToken;
#endif

    private NetTransport transport;

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

    void OnDestroy()
    {
        if (transport != null)
        {
            RemoveTransport();
        }
    }

    void FixedUpdate()
    {
        ClientTick++;
    }

    private void HandleNetworkConnected(NetTransport transport, ConnectedArgs args)
    {
        if (GameResources.Instance.GameMode == GameMode.Singleplayer)
        {
            transport.SendToAll(PacketBuilder.ConnectionRequest(ConnectionData), TransportMethod.Reliable);
            return;
        }

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        transport.SendToAll(PacketBuilder.ConnectionRequest(ConnectionToken), TransportMethod.Reliable);
#elif CNS_SERVER_MULTIPLE && CNS_SYNC_HOST
        if (lobbyConnectionType == LobbyConnectionType.CREATE)
        {
            transport.SendToAll(PacketBuilder.ConnectionRequest(ConnectionData), TransportMethod.Reliable);
        }
        else
        {
            transport.SendToAll(PacketBuilder.ConnectionRequest(ConnectionToken), TransportMethod.Reliable);
        }
#elif CNS_SERVER_SINGLE
        transport.SendToAll(PacketBuilder.ConnectionRequest(ConnectionData), TransportMethod.Reliable);
#endif
    }

    private void HandleNetworkDisconnected(NetTransport transport, DisconnectedArgs args)
    {
        Debug.Log("<color=green><b>CNS</b></color>: Client disconnected");
        LeaveCurrentLobby();
        IsConnected = false;
        OnLobbyConnectionLost?.Invoke();
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
                int lobbyId = packet.ReadInt();
                if (accepted)
                {
                    Debug.Log("<color=green><b>CNS</b></color>: Client connected");
                    CurrentLobby.LobbyData.LobbyId = lobbyId;
                    CurrentUser.LobbyId = lobbyId;
                    IsConnected = true;
                    OnLobbyConnectionAccepted?.Invoke(CurrentLobby.LobbyData.LobbyId);
                }
                else
                {
                    Debug.LogWarning("<color=yellow><b>CNS</b></color>: Client rejected");
                    LobbyRejectionType errorType = (LobbyRejectionType)packet.ReadByte();
                    OnLobbyConnectionRejected?.Invoke(CurrentLobby.LobbyData.LobbyId, errorType);
                }
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received packet with unknown service type {serviceType} and command type {commandType} while not in a lobby.");
            }
        }
    }

    public void CreateNewUser(UserSettings userSettings = null, bool invokeEvent = true)
    {
        if (GameResources.Instance.GameMode == GameMode.Singleplayer)
        {
            CreateUser(Guid.NewGuid(), userSettings ?? GameResources.Instance.DefaultUserSettings, invokeEvent);
            return;
        }

#if CNS_SERVER_MULTIPLE
        StartCoroutine(CreateUserCoroutine(userSettings, invokeEvent));
#elif CNS_SERVER_SINGLE
        CreateUser(Guid.NewGuid(), userSettings ?? GameResources.Instance.DefaultUserSettings, invokeEvent);
#endif
    }

#if CNS_SERVER_MULTIPLE
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
                webToken = userResponse.Token;
                CreateUser(userResponse.GlobalGuid, userResponse.UserSettings, invokeEvent);
            }
            else
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: Failed to create user: {www.error}");
            }
        }
    }
#endif

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
#if !(CNS_SERVER_MULTIPLE && CNS_SYNC_HOST)
        else
        {
            UpdateUser(userSettings, invokeEvent);
        }
#endif

#if CNS_SERVER_MULTIPLE && CNS_SYNC_HOST
        if (GameResources.Instance.GameMode != GameMode.Singleplayer)
        {
            StartCoroutine(UpdateUserCoroutine(userSettings, !IsConnected && invokeEvent));
        }
#endif
    }

#if CNS_SERVER_MULTIPLE && CNS_SYNC_HOST
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
                UpdateUser(userSettings, invokeEvent);
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

    private void UpdateUser(UserSettings userSettings, bool invokeEvent)
    {
        CurrentUser.Settings = userSettings;
        if (invokeEvent)
        {
            OnCurrentUserUpdated?.Invoke(userSettings);
        }
    }

    public void CreateNewLobby(LobbySettings lobbySettings = null, bool invokeEvent = true)
    {
        if (GameResources.Instance.GameMode == GameMode.Singleplayer)
        {
            CreateLobby(GameResources.Instance.DefaultLobbyId, lobbySettings ?? GameResources.Instance.DefaultLobbySettings, null, invokeEvent);
            return;
        }

#if CNS_SERVER_MULTIPLE
        StartCoroutine(CreateLobbyCoroutine(lobbySettings, invokeEvent));
#elif CNS_SERVER_SINGLE
        CreateLobby(-1, lobbySettings ?? GameResources.Instance.DefaultLobbySettings, null, invokeEvent);
#endif
    }

#if CNS_SERVER_MULTIPLE
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
                CurrentServerSettings = lobbyResponse.ServerSettings;
                ConnectionToken = lobbyResponse.ServerToken;
                CreateLobby(lobbyResponse.LobbyId, lobbyResponse.LobbySettings, lobbyResponse.ServerSettings, invokeEvent);
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

#nullable enable
    private void CreateLobby(int lobbyId, LobbySettings lobbySettings, ServerSettings? serverSettings, bool invokeEvent)
    {
        ConnectionData = new ConnectionData
        {
            LobbyId = lobbyId,
            LobbyConnectionType = LobbyConnectionType.CREATE,
            UserGuid = CurrentUser.GlobalGuid,
            UserSettings = CurrentUser.Settings,
            LobbySettings = lobbySettings
        };
        CurrentLobby.LobbyData.Settings = lobbySettings;
        if (invokeEvent)
        {
            OnLobbyCreateRequested?.Invoke(serverSettings);
        }
    }
#nullable disable

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
#if CNS_SYNC_DEDICATED || (CNS_SERVER_SINGLE && CNS_SYNC_HOST)
        else
        {
            UpdateLobby(lobbySettings, invokeEvent);
        }
#endif

#if CNS_SERVER_MULTIPLE && CNS_SYNC_HOST
        if (GameResources.Instance.GameMode != GameMode.Singleplayer)
        {
            StartCoroutine(UpdateLobbyCoroutine(lobbySettings, !IsConnected && invokeEvent));
        }
#endif
    }

#if CNS_SERVER_MULTIPLE && CNS_SYNC_HOST
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
                UpdateLobby(lobbySettings, invokeEvent);
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

    private void UpdateLobby(LobbySettings lobbySettings, bool invokeEvent)
    {
        CurrentLobby.LobbyData.Settings = lobbySettings;
        if (invokeEvent)
        {
            OnCurrentLobbyUpdated?.Invoke(lobbySettings);
        }
    }

    public void JoinExistingLobby(int lobbyId, bool invokeEvent = true)
    {
        if (GameResources.Instance.GameMode == GameMode.Singleplayer)
        {
            JoinLobby(lobbyId, GameResources.Instance.DefaultLobbySettings, null, invokeEvent);
            return;
        }

#if CNS_SERVER_MULTIPLE
        StartCoroutine(JoinLobbyCoroutine(lobbyId, invokeEvent));
#elif CNS_SERVER_SINGLE
        JoinLobby(lobbyId, GameResources.Instance.DefaultLobbySettings, null, invokeEvent);
#endif
    }

#if CNS_SERVER_MULTIPLE
    private IEnumerator JoinLobbyCoroutine(int lobbyId, bool invokeEvent)
    {
        using (var www = new UnityWebRequest($"{LobbyApiUrl}/lobby/join/{lobbyId}", "GET"))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Authorization", $"Bearer {webToken}");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var lobbyResponse = JsonConvert.DeserializeObject<LobbyResponse>(www.downloadHandler.text);
                CurrentServerSettings = lobbyResponse.ServerSettings;
                ConnectionToken = lobbyResponse.ServerToken;
                JoinLobby(lobbyResponse.LobbyId, lobbyResponse.LobbySettings, lobbyResponse.ServerSettings, invokeEvent);
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

#nullable enable
    private void JoinLobby(int lobbyId, LobbySettings lobbySettings, ServerSettings? serverSettings, bool invokeEvent)
    {
        ConnectionData = new ConnectionData
        {
            LobbyId = lobbyId,
            LobbyConnectionType = LobbyConnectionType.CREATE,
            UserGuid = CurrentUser.GlobalGuid,
            UserSettings = CurrentUser.Settings,
            LobbySettings = lobbySettings
        };
        CurrentLobby.LobbyData.Settings = lobbySettings;
        if (invokeEvent)
        {
            OnLobbyJoinRequested?.Invoke(lobbyId, serverSettings);
        }
    }
#nullable disable

    public void LeaveCurrentLobby()
    {
        if (transport != null)
        {
            transport.Shutdown();
        }
    }

    public void SetClientTick(ulong tick)
    {
        ClientTick = tick;
    }

    public void SetCurrentUserData(UserData userData)
    {
        CurrentUser = userData;
    }

    public void RegisterTransport(TransportType transportType)
    {
        if (transport != null)
        {
            RemoveTransport();
        }

        string transportName = null;
        switch (transportType)
        {
#if CNS_TRANSPORT_LOCAL
            case TransportType.Local:
                transportName = "LocalTransport";
                break;
#endif
#if CNS_TRANSPORT_CNET
            case TransportType.CNet:
                transportName = "CNetTransport";
                break;
#endif
#if CNS_TRANSPORT_CNET && CNS_TRANSPORT_CNETRELAY && CNS_SYNC_HOST
            case TransportType.CNetRelay:
                transportName = "CNetRelayTransport";
                break;
#endif
#if CNS_TRANSPORT_LITENETLIB
            case TransportType.LiteNetLib:
                transportName = "LiteNetLibTransport";
                break;
#endif
#if CNS_TRANSPORT_LITENETLIB && CNS_TRANSPORT_LITENETLIBRELAY && CNS_SYNC_HOST
            case TransportType.LiteNetLibRelay:
                transportName = "LiteNetLibRelayTransport";
                break;
#endif
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
            case TransportType.SteamRelay:
                transportName = "SteamRelayTransport";
                break;
#endif
        }

        transport = Instantiate(Resources.Load<NetTransport>($"Prefabs/Transports/{transportName}"), this.transform);
        AddTransportEvents();
        transport.Initialize(NetDeviceType.Client);
        transport.StartDevice();
        CurrentLobby.Init(CurrentLobby.LobbyData.LobbyId, transport);
    }

    public void SetTransport(NetTransport newTransport)
    {
        if (transport != null)
        {
            RemoveTransport();
        }

        transport = newTransport;
        transport.transform.SetParent(this.transform);
        AddTransportEvents();
    }

#if CNS_SYNC_HOST
    public void BridgeTransport()
    {
        if (transport == null)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Attempted to bridge a null transport.");
            return;
        }

        if (ServerManager.Instance == null)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Attempted to bridge transport but ServerManager instance is null.");
            return;
        }

        ClearTransportEvents();
        ServerManager.Instance.AddTransport(transport);
        transport = null;
    }
#endif

    public void AddTransportEvents()
    {
        transport.OnNetworkConnected += HandleNetworkConnected;
        transport.OnNetworkDisconnected += HandleNetworkDisconnected;
        transport.OnNetworkReceived += HandleNetworkReceived;
    }

    public void ClearTransportEvents()
    {
        transport.OnNetworkConnected -= HandleNetworkConnected;
        transport.OnNetworkDisconnected -= HandleNetworkDisconnected;
        transport.OnNetworkReceived -= HandleNetworkReceived;
    }

    public void RemoveTransport()
    {
        ClearTransportEvents();
        transport.Shutdown();
        Destroy(transport.gameObject);
        transport = null;
    }
}
