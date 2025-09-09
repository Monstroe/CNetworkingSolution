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
#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_HOST
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

    public delegate void OnLobbyConnectionAcceptedEventHandler(int lobbyId);
    public event OnLobbyConnectionAcceptedEventHandler OnLobbyConnectionAccepted;

    public delegate void OnLobbyConnectionRejectedEventHandler(int lobbyId, LobbyRejectionType errorType);
    public event OnLobbyConnectionRejectedEventHandler OnLobbyConnectionRejected;

    public static ClientManager Instance { get; private set; }

    public ulong ClientTick { get; private set; } = 0;
    public UserData CurrentUser { get; private set; }
    public ClientLobby CurrentLobby { get; private set; }
    public bool IsConnected { get; private set; } = false;

    [SerializeField] private NetTransport transport;
    private LobbyConnectionType lobbyConnectionType;

#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_HOST
    public string LobbyApiUrl
    {
        get => lobbyApiUrl;
        set => lobbyApiUrl = value;
    }
    [Tooltip("The URL of the lobby API. PLEASE DON'T PUT A SLASH AT THE END.")]
    [SerializeField] private string lobbyApiUrl = "http://localhost:5107/api";
#endif

#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_HOST
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

        if (transport)
        {
            transport.OnNetworkConnected += HandleNetworkConnected;
            transport.OnNetworkDisconnected += HandleNetworkDisconnected;
            transport.OnNetworkReceived += HandleNetworkReceived;
        }
    }

    void Oestroy()
    {
        if (transport)
        {
            transport.OnNetworkConnected -= HandleNetworkConnected;
            transport.OnNetworkDisconnected -= HandleNetworkDisconnected;
            transport.OnNetworkReceived -= HandleNetworkReceived;
            transport.Shutdown();
        }
    }

    void FixedUpdate()
    {
        ClientTick++;
    }

    private void HandleNetworkConnected(NetTransport transport, ConnectedArgs args)
    {
        if (GameResources.Instance.GameMode == GameMode.SINGLEPLAYER)
        {
            transport.SendToAll(PacketBuilder.ConnectionRequest(new ConnectionData
            {
                LobbyId = CurrentLobby.LobbyData.LobbyId,
                LobbyConnectionType = lobbyConnectionType,
                UserGuid = CurrentUser.GlobalGuid,
                UserSettings = CurrentUser.Settings,
                LobbySettings = CurrentLobby.LobbyData.Settings
            }), TransportMethod.Reliable);
            return;
        }

#if CNS_SYNC_SERVER_MULTIPLE
        transport.SendToAll(PacketBuilder.ConnectionRequest(serverToken), TransportMethod.Reliable);
#elif CNS_SYNC_SERVER_SINGLE || CNS_SYNC_HOST
        transport.SendToAll(PacketBuilder.ConnectionRequest(new ConnectionData
        {
            LobbyId = CurrentLobby.LobbyData.LobbyId,
            LobbyConnectionType = lobbyConnectionType,
            UserGuid = CurrentUser.GlobalGuid,
            UserSettings = CurrentUser.Settings,
            LobbySettings = CurrentLobby.LobbyData.Settings
        }), TransportMethod.Reliable);
#endif
    }

    private void HandleNetworkDisconnected(NetTransport transport, DisconnectedArgs args)
    {
        Debug.Log("<color=green><b>CNS</b></color>: Client disconnected");
        transport.Shutdown();
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
        if (GameResources.Instance.GameMode == GameMode.SINGLEPLAYER)
        {
            CreateUser(Guid.NewGuid(), userSettings ?? GameResources.Instance.DefaultUserSettings, invokeEvent);
            return;
        }

#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_HOST
        StartCoroutine(CreateUserCoroutine(userSettings, invokeEvent));
#elif CNS_SYNC_SERVER_SINGLE
        CreateUser(Guid.NewGuid(), userSettings ?? GameResources.Instance.DefaultUserSettings, invokeEvent);
#endif
    }

#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_HOST
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
#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_SERVER_SINGLE
        else
        {
            UpdateUser(userSettings, invokeEvent);
        }
#endif

#if CNS_SYNC_HOST
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            StartCoroutine(UpdateUserCoroutine(userSettings, !IsConnected && invokeEvent));
        }
#endif
    }

#if CNS_SYNC_HOST
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
        if (GameResources.Instance.GameMode == GameMode.SINGLEPLAYER)
        {
            CreateLobby(GameResources.Instance.DefaultLobbyId, lobbySettings ?? GameResources.Instance.DefaultLobbySettings, null, invokeEvent);
            return;
        }

#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_HOST
        StartCoroutine(CreateLobbyCoroutine(lobbySettings, invokeEvent));
#elif CNS_SYNC_SERVER_SINGLE
        CreateLobby(-1, lobbySettings ?? GameResources.Instance.DefaultLobbySettings, null, invokeEvent);
#endif
    }

#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_HOST
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
                serverToken = lobbyResponse.ServerToken;
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
        lobbyConnectionType = LobbyConnectionType.CREATE;
        CurrentLobby.Init(lobbyId, transport);
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
#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_SERVER_SINGLE
        else
        {
            UpdateLobby(lobbySettings, invokeEvent);
        }
#endif

#if CNS_SYNC_HOST
        if (GameResources.Instance.GameMode != GameMode.SINGLEPLAYER)
        {
            StartCoroutine(UpdateLobbyCoroutine(lobbySettings, !IsConnected && invokeEvent));
        }
#endif
    }

#if CNS_SYNC_HOST
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
        if (GameResources.Instance.GameMode == GameMode.SINGLEPLAYER)
        {
            JoinLobby(lobbyId, GameResources.Instance.DefaultLobbySettings, null, invokeEvent);
            return;
        }

#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_HOST
        StartCoroutine(JoinLobbyCoroutine(lobbyId, invokeEvent));
#elif CNS_SYNC_SERVER_SINGLE
        JoinLobby(lobbyId, GameResources.Instance.DefaultLobbySettings, null, invokeEvent);
#endif
    }

#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_HOST
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
                serverToken = lobbyResponse.ServerToken;
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
        lobbyConnectionType = LobbyConnectionType.JOIN;
        CurrentLobby.Init(lobbyId, transport);
        CurrentLobby.LobbyData.Settings = lobbySettings;
        if (invokeEvent)
        {
            OnLobbyJoinRequested?.Invoke(lobbyId, serverSettings);
        }
    }
#nullable disable

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
        ClearTransport();

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

        transport.OnNetworkConnected += HandleNetworkConnected;
        transport.OnNetworkDisconnected += HandleNetworkDisconnected;
        transport.OnNetworkReceived += HandleNetworkReceived;
    }

    private void ClearTransport()
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
