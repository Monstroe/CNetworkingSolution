using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

[RequireComponent(typeof(SingleTransportUtility))]
[RequireComponent(typeof(ClientLobby))]
public class ClientManager : MonoBehaviour
{
    public delegate void NewUserCreatedEventHandler(Guid userId);
    public event NewUserCreatedEventHandler OnNewUserCreated;

#nullable enable
    public delegate void LobbyCreateRequestedEventHandler(TransportSettings? serverSettings);
#nullable disable
    public event LobbyCreateRequestedEventHandler OnLobbyCreateRequested;

#nullable enable
    public delegate void LobbyJoinRequestedEventHandler(int lobbyId, TransportSettings? serverSettings);
#nullable disable
    public event LobbyJoinRequestedEventHandler OnLobbyJoinRequested;

    public delegate void LobbyConnectionAcceptedEventHandler(int lobbyId);
    public event LobbyConnectionAcceptedEventHandler OnLobbyConnectionAccepted;

    public delegate void LobbyConnectionRejectedEventHandler(int lobbyId, LobbyRejectionType errorType);
    public event LobbyConnectionRejectedEventHandler OnLobbyConnectionRejected;

    public delegate void LobbyConnectionLostEventHandler(TransportCode code);
    public event LobbyConnectionLostEventHandler OnLobbyConnectionLost;

    public delegate void LobbyConnectionErrorEventHandler(TransportCode code, SocketError? socketError);
    public event LobbyConnectionErrorEventHandler OnLobbyConnectionError;

    public static ClientManager Instance { get; private set; }
    public ClientLobby CurrentLobby { get; private set; }
#nullable enable
    public TransportSettings? CurrentTransportSettings { get; set; }
#nullable disable
    public bool IsConnected { get; private set; } = false;
    public ConnectionData ConnectionData { get; private set; }
    public NetMode NetMode { get; set; }

#if CNS_SERVER_MULTIPLE
    [Tooltip("The URL of the lobby API. PLEASE DON'T PUT A SLASH AT THE END.")]
    [SerializeField] private string lobbyApiUrl = "http://localhost:5107/api";

    public ClientWebAPI WebAPI { get; private set; }
#endif

    private SingleTransportUtility transportUtility;

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
            return;
        }

        Debug.Log("<color=green><b>CNS</b></color>: Initializing Client...");

        transportUtility = GetComponent<SingleTransportUtility>();
        AddTransportUtilityEvents();
        CurrentLobby = GetComponent<ClientLobby>();
        CurrentLobby.Init(transportUtility);
        CurrentLobby.LobbyData.Settings = NetResources.Instance.DefaultLobbySettings.Clone();
        CurrentLobby.CurrentUser.Settings = NetResources.Instance.DefaultUserSettings.Clone();
        NetMode = NetResources.Instance.DefaultNetMode;
#if CNS_SERVER_MULTIPLE
        WebAPI = new ClientWebAPI(lobbyApiUrl);
#endif
        Debug.Log("<color=green><b>CNS</b></color>: Client initialized.");
    }

    void OnDestroy()
    {
        transportUtility.RemoveTransports();
        ClearTransportUtilityEvents();
    }

    private void HandleNetworkConnected(ulong remoteId)
    {
        if (NetMode == NetMode.Local)
        {
            transportUtility.SendToAllRemotes(PacketBuilder.ConnectionRequest(ConnectionData), TransportMethod.Reliable);
            return;
        }
#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        if (WebAPI.ConnectionToken != null)
        {
            transportUtility.SendToAllRemotes(PacketBuilder.ConnectionRequest(WebAPI.ConnectionToken), TransportMethod.Reliable);
        }
        else
        {
            transportUtility.SendToAllRemotes(PacketBuilder.ConnectionRequest(ConnectionData), TransportMethod.Reliable);
        }
#elif CNS_SERVER_MULTIPLE && CNS_SYNC_HOST
        if (ConnectionData.LobbyConnectionType == LobbyConnectionType.Create)
        {
            transportUtility.SendToAllRemotes(PacketBuilder.ConnectionRequest(ConnectionData), TransportMethod.Reliable);
        }
        else
        {
            if (WebAPI.ConnectionToken != null)
            {
                transportUtility.SendToAllRemotes(PacketBuilder.ConnectionRequest(WebAPI.ConnectionToken), TransportMethod.Reliable);
            }
            else
            {
                transportUtility.SendToAllRemotes(PacketBuilder.ConnectionRequest(ConnectionData), TransportMethod.Reliable);
            }
        }
#elif CNS_SERVER_SINGLE
            transportUtility.SendToAllRemotes(PacketBuilder.ConnectionRequest(ConnectionData), TransportMethod.Reliable);
#endif
    }

    private void HandleNetworkDisconnected(ulong remoteId, TransportCode code)
    {
        Debug.Log("<color=green><b>CNS</b></color>: Client disconnected: " + code);
        transportUtility.RemoveTransports();
        IsConnected = false;
        CurrentLobby.LobbyData = new LobbyData();
        CurrentLobby.LobbyData.Settings = NetResources.Instance.DefaultLobbySettings.Clone();
        OnLobbyConnectionLost?.Invoke(code);
    }

    private void HandleNetworkReceived(ulong remoteId, NetPacket packet, TransportMethod? method)
    {
#if !UNITY_EDITOR
        try
        {
#endif
        if (CurrentLobby.CurrentUser.InLobby)
        {
            CurrentLobby.ReceiveData(packet, method);
        }
        else
        {
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
                    CurrentLobby.CurrentUser.LobbyId = lobbyId;
                    IsConnected = true;
                    OnLobbyConnectionAccepted?.Invoke(CurrentLobby.LobbyData.LobbyId);
                }
                else
                {
                    Debug.LogWarning("<color=yellow><b>CNS</b></color>: Client rejected");
                    LobbyRejectionType errorType = (LobbyRejectionType)packet.ReadByte();
                    OnLobbyConnectionRejected?.Invoke(lobbyId, errorType);
                }
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received packet with unknown service type {serviceType} and command type {commandType} while not in a lobby.");
            }
        }
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error when processing received data from server: {ex.Message}");
        }
#endif
    }

    private void HandleNetworkReceivedUnconnected(IPEndPoint iPEndPoint, NetPacket packet)
    {
#if !UNITY_EDITOR
        try
        {
#endif
        CurrentLobby.ReceiveDataUnconnected(iPEndPoint, packet);
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Unknown error when processing unconnected received data from {iPEndPoint}: {ex.Message}");
        }
#endif
    }

    private void HandleNetworkError(TransportCode code, SocketError? socketError)
    {
        Debug.LogError($"<color=red><b>CNS</b></color>: Network error occurred: {code} {(socketError.HasValue ? $"(Socket Error: {socketError.Value})" : "")}");
        OnLobbyConnectionError?.Invoke(code, socketError);
    }

    public void CreateNewUser(UserSettings userSettings = null, bool invokeEvent = true)
    {
        if (NetMode == NetMode.Local)
        {
            CreateUser(Guid.NewGuid(), userSettings ?? NetResources.Instance.DefaultUserSettings.Clone(), invokeEvent);
            return;
        }

#if CNS_SERVER_MULTIPLE
        StartCoroutine(WebAPI.CreateUserCoroutine(userSettings ?? NetResources.Instance.DefaultUserSettings.Clone(), (userGuid, settings) =>
        {
            CreateUser(userGuid, settings, invokeEvent);
        }));
#elif CNS_SERVER_SINGLE
        CreateUser(Guid.NewGuid(), userSettings ?? NetResources.Instance.DefaultUserSettings.Clone(), invokeEvent);
#endif
    }

    private void CreateUser(Guid userGuid, UserSettings userSettings, bool invokeEvent)
    {
        UserData userData = new UserData
        {
            GlobalGuid = userGuid,
            Settings = userSettings
        };
        CurrentLobby.CurrentUser = userData;
        if (invokeEvent)
        {
            OnNewUserCreated?.Invoke(CurrentLobby.CurrentUser.GlobalGuid);
        }
    }

    public void UpdateCurrentUser(UserSettings userSettings)
    {
        if (IsConnected)
        {
            CurrentLobby.SendToServer(PacketBuilder.LobbyUserSettings(CurrentLobby.CurrentUser, userSettings), TransportMethod.Reliable);
        }
        else if (NetMode == NetMode.Local)
        {
            UpdateUser(userSettings);
        }
#if !(CNS_SERVER_MULTIPLE && CNS_SYNC_HOST)
        else
        {
            UpdateUser(userSettings);
        }
#endif

#if CNS_SERVER_MULTIPLE && CNS_SYNC_HOST
        if (NetMode != NetMode.Local)
        {
            StartCoroutine(WebAPI.UpdateUserCoroutine(userSettings, (userGuid, settings) =>
            {
                // User recreated
                CreateUser(userGuid, settings, false);
            }, (updatedSettings) =>
            {
                // User updated
                UpdateUser(updatedSettings);
            }));
        }
#endif
    }

    private void UpdateUser(UserSettings userSettings)
    {
        CurrentLobby.CurrentUser.Settings = userSettings;
    }

    public void CreateNewLobby(LobbySettings lobbySettings = null, bool invokeEvent = true)
    {
        if (NetMode == NetMode.Local)
        {
            CreateLobby(NetResources.Instance.DefaultLobbyId, lobbySettings ?? NetResources.Instance.DefaultLobbySettings.Clone(), null, invokeEvent);
            return;
        }

#if CNS_SERVER_MULTIPLE
        StartCoroutine(WebAPI.CreateLobbyCoroutine(lobbySettings, CurrentLobby.CurrentUser.Settings, (userGuid, settings) =>
        {
            // User recreated
            CreateUser(userGuid, settings, false);
        }, (lobbyId, lobbySettingsResponse, serverSettingsResponse) =>
        {
            // Lobby created
            CreateLobby(lobbyId, lobbySettingsResponse, serverSettingsResponse, invokeEvent);
        }));
#elif CNS_SERVER_SINGLE
        CreateLobby(-1, lobbySettings ?? NetResources.Instance.DefaultLobbySettings.Clone(), null, invokeEvent);
#endif
    }

#nullable enable
    private void CreateLobby(int lobbyId, LobbySettings lobbySettings, TransportSettings? serverSettings, bool invokeEvent)
    {
        ConnectionData = new ConnectionData
        {
            LobbyId = lobbyId,
            LobbyConnectionType = LobbyConnectionType.Create,
            UserGuid = CurrentLobby.CurrentUser.GlobalGuid,
            UserSettings = CurrentLobby.CurrentUser.Settings,
            LobbySettings = lobbySettings
        };
        CurrentLobby.LobbyData.LobbyId = lobbyId;
        CurrentLobby.LobbyData.Settings = lobbySettings;
        if (invokeEvent)
        {
            OnLobbyCreateRequested?.Invoke(serverSettings);
        }
    }
#nullable disable

    public void UpdateCurrentLobby(LobbySettings lobbySettings)
    {
        if (IsConnected)
        {
            CurrentLobby.SendToServer(PacketBuilder.LobbySettings(lobbySettings), TransportMethod.Reliable);
        }
        else if (NetMode == NetMode.Local)
        {
            UpdateLobby(lobbySettings);
        }
#if !(CNS_SERVER_MULTIPLE && CNS_SYNC_HOST)
        else
        {
            UpdateLobby(lobbySettings);
        }
#endif

#if CNS_SERVER_MULTIPLE && CNS_SYNC_HOST
        if (NetMode != NetMode.Local)
        {
            StartCoroutine(WebAPI.UpdateLobbyCoroutine(lobbySettings, CurrentLobby.CurrentUser.Settings, (userGuid, settings) =>
            {
                // User recreated
                CreateUser(userGuid, settings, false);
            }, (updatedLobbySettings) =>
            {
                // Lobby updated
                UpdateLobby(updatedLobbySettings);
            }));
        }
#endif
    }

    private void UpdateLobby(LobbySettings lobbySettings)
    {
        CurrentLobby.LobbyData.Settings = lobbySettings;
    }

    public void JoinExistingLobby(int lobbyId, bool invokeEvent = true)
    {
        if (NetMode == NetMode.Local)
        {
            JoinLobby(lobbyId, NetResources.Instance.DefaultLobbySettings.Clone(), null, invokeEvent);
            return;
        }

#if CNS_SERVER_MULTIPLE
        StartCoroutine(WebAPI.JoinLobbyCoroutine(lobbyId, CurrentLobby.CurrentUser.Settings, (userGuid, settings) =>
        {
            // User recreated
            CreateUser(userGuid, settings, false);
        }, (joinedLobbyId, lobbySettingsResponse, serverSettingsResponse) =>
        {
            // Lobby joined
            JoinLobby(joinedLobbyId, lobbySettingsResponse, serverSettingsResponse, invokeEvent);
        }));
#elif CNS_SERVER_SINGLE
        JoinLobby(lobbyId, NetResources.Instance.DefaultLobbySettings.Clone(), null, invokeEvent);
#endif
    }

#nullable enable
    private void JoinLobby(int lobbyId, LobbySettings lobbySettings, TransportSettings? serverSettings, bool invokeEvent)
    {
        ConnectionData = new ConnectionData
        {
            LobbyId = lobbyId,
            LobbyConnectionType = LobbyConnectionType.Join,
            UserGuid = CurrentLobby.CurrentUser.GlobalGuid,
            UserSettings = CurrentLobby.CurrentUser.Settings,
            LobbySettings = lobbySettings
        };
        CurrentLobby.LobbyData.LobbyId = lobbyId;
        CurrentLobby.LobbyData.Settings = lobbySettings;
        if (invokeEvent)
        {
            OnLobbyJoinRequested?.Invoke(lobbyId, serverSettings);
        }
    }
#nullable disable

    public void RemoveTransports()
    {
        transportUtility.RemoveTransports();
    }

#nullable enable
    public void RegisterTransport(TransportType transportType, TransportSettings? transportSettings = null)
    {
        CurrentTransportSettings = transportSettings;
        transportUtility.RegisterTransport(transportType, NetDeviceType.Client, transportSettings);
    }
#nullable disable

    public void SetTransport(NetTransport newTransport)
    {
        transportUtility.AddTransport(newTransport);
    }

#if CNS_SYNC_HOST && CNS_LOBBY_MULTIPLE
    public void BridgeTransport()
    {
        if (transportUtility.Transport == null)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Attempted to bridge a null Transport.");
            return;
        }

        if (ServerManager.Instance == null)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Attempted to bridge Transport but ServerManager instance is null.");
            return;
        }

        transportUtility.ClearTransportEvents();
        ServerManager.Instance.AddTransport(transportUtility.Transport);
        transportUtility.Transport = null;
    }
#endif

    private void AddTransportUtilityEvents()
    {
        transportUtility.OnSingleConnected += HandleNetworkConnected;
        transportUtility.OnSingleDisconnected += HandleNetworkDisconnected;
        transportUtility.OnSingleReceived += HandleNetworkReceived;
        transportUtility.OnSingleReceivedUnconnected += HandleNetworkReceivedUnconnected;
        transportUtility.OnSingleError += HandleNetworkError;
    }

    private void ClearTransportUtilityEvents()
    {
        transportUtility.OnSingleConnected -= HandleNetworkConnected;
        transportUtility.OnSingleDisconnected -= HandleNetworkDisconnected;
        transportUtility.OnSingleReceived -= HandleNetworkReceived;
        transportUtility.OnSingleReceivedUnconnected -= HandleNetworkReceivedUnconnected;
        transportUtility.OnSingleError -= HandleNetworkError;
    }
}
