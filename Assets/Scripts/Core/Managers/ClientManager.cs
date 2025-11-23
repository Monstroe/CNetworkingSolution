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
    public delegate void LobbyCreateRequestedEventHandler(ServerSettings? serverSettings);
#nullable disable
    public event LobbyCreateRequestedEventHandler OnLobbyCreateRequested;

#nullable enable
    public delegate void LobbyJoinRequestedEventHandler(int lobbyId, ServerSettings? serverSettings);
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
    public ServerSettings? CurrentServerSettings { get; set; }
#nullable disable
    public bool IsConnected { get; private set; } = false;
    public ConnectionData ConnectionData { get; private set; }

#if CNS_SERVER_MULTIPLE
    [Tooltip("The URL of the lobby API. PLEASE DON'T PUT A SLASH AT THE END.")]
    [SerializeField] private string lobbyApiUrl = "http://localhost:5107/api";

    private ClientWebAPI webAPI;
#endif

    private SingleTransportUtility transportUtility;
    private readonly ClientServiceUtility unconnectedServices = new ClientServiceUtility();

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
#if CNS_SERVER_MULTIPLE
        webAPI = new ClientWebAPI(lobbyApiUrl);
#endif
        Debug.Log("<color=green><b>CNS</b></color>: Client initialized.");
    }

    void OnDestroy()
    {
        transportUtility.RemoveTransport();
        ClearTransportUtilityEvents();
    }

    private void HandleNetworkConnected(uint remoteId)
    {
        if (NetResources.Instance.GameMode == GameMode.Singleplayer)
        {
            transportUtility.SendToAllRemotes(PacketBuilder.ConnectionRequest(ConnectionData), TransportMethod.Reliable);
            return;
        }

#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
        transportUtility.SendToAllRemotes(PacketBuilder.ConnectionRequest(webAPI.ConnectionToken), TransportMethod.Reliable);
#elif CNS_SERVER_MULTIPLE && CNS_SYNC_HOST
        if (ConnectionData.LobbyConnectionType == LobbyConnectionType.Create)
        {
            transportUtility.SendToAllRemotes(PacketBuilder.ConnectionRequest(ConnectionData), TransportMethod.Reliable);
        }
        else
        {
            transportUtility.SendToAllRemotes(PacketBuilder.ConnectionRequest(webAPI.ConnectionToken), TransportMethod.Reliable);
        }
#elif CNS_SERVER_SINGLE
        transportUtility.SendToAllRemotes(PacketBuilder.ConnectionRequest(ConnectionData), TransportMethod.Reliable);
#endif
    }

    private void HandleNetworkDisconnected(uint remoteId, TransportCode code)
    {
        Debug.Log("<color=green><b>CNS</b></color>: Client disconnected: " + code);
        transportUtility.RemoveTransport();
        IsConnected = false;
        OnLobbyConnectionLost?.Invoke(code);
    }

    private void HandleNetworkReceived(uint remoteId, NetPacket packet, TransportMethod? method)
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
                    OnLobbyConnectionRejected?.Invoke(CurrentLobby.LobbyData.LobbyId, errorType);
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
        ServiceType serviceType = (ServiceType)packet.ReadByte();
        CommandType commandType = (CommandType)packet.ReadByte();

        if (unconnectedServices.GetService(serviceType, out ClientService service))
        {
            service.ReceiveDataUnconnected(iPEndPoint, packet, serviceType, commandType);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No unconnected service found for type {serviceType}. Command {commandType} will not be processed.");
        }
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

    public void SendToUnconnectedClient(IPEndPoint iPEndPoint, NetPacket packet)
    {
        if (packet != null)
        {
            transportUtility.SendToUnconnectedRemote(iPEndPoint, packet);
        }
    }

    public void SendToUnconnectedClients(List<IPEndPoint> iPEndPoints, NetPacket packet)
    {
        if (packet != null)
        {
            transportUtility.SendToUnconnectedRemotes(iPEndPoints, packet);
        }
    }

    public void BroadcastToUnconnectedClients(NetPacket packet)
    {
        if (packet != null)
        {
            transportUtility.BroadcastToUnconnectedRemotes(packet);
        }
    }

    public void CreateNewUser(UserSettings userSettings = null, bool invokeEvent = true)
    {
        if (NetResources.Instance.GameMode == GameMode.Singleplayer)
        {
            CreateUser(Guid.NewGuid(), userSettings ?? NetResources.Instance.DefaultUserSettings, invokeEvent);
            return;
        }

#if CNS_SERVER_MULTIPLE
        StartCoroutine(webAPI.CreateUserCoroutine(userSettings ?? NetResources.Instance.DefaultUserSettings, (userGuid, settings) =>
        {
            CreateUser(userGuid, settings, invokeEvent);
        }));
#elif CNS_SERVER_SINGLE
        CreateUser(Guid.NewGuid(), userSettings ?? NetResources.Instance.DefaultUserSettings, invokeEvent);
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
#if !(CNS_SERVER_MULTIPLE && CNS_SYNC_HOST)
        else
        {
            UpdateUser(userSettings);
        }
#endif

#if CNS_SERVER_MULTIPLE && CNS_SYNC_HOST
        if (NetResources.Instance.GameMode != GameMode.Singleplayer)
        {
            StartCoroutine(webAPI.UpdateUserCoroutine(userSettings, (userGuid, settings) =>
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

    public void CreateNewLobby(LobbySettings lobbySettings = null, ServerSettings serverSettings = null, bool invokeEvent = true)
    {
        if (NetResources.Instance.GameMode == GameMode.Singleplayer)
        {
            CreateLobby(NetResources.Instance.DefaultLobbyId, lobbySettings ?? NetResources.Instance.DefaultLobbySettings, serverSettings, invokeEvent);
            return;
        }

#if CNS_SERVER_MULTIPLE
        StartCoroutine(webAPI.CreateLobbyCoroutine(lobbySettings, CurrentLobby.CurrentUser.Settings, (userGuid, settings) =>
        {
            // User recreated
            CreateUser(userGuid, settings, false);
        }, (lobbyId, lobbySettingsResponse, serverSettingsResponse) =>
        {
            // Lobby created
            CreateLobby(lobbyId, lobbySettingsResponse, serverSettingsResponse, invokeEvent);
        }));
#elif CNS_SERVER_SINGLE
        CreateLobby(-1, lobbySettings ?? NetResources.Instance.DefaultLobbySettings, serverSettings, invokeEvent);
#endif
    }

#nullable enable
    private void CreateLobby(int lobbyId, LobbySettings lobbySettings, ServerSettings? serverSettings, bool invokeEvent)
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
        CurrentServerSettings = serverSettings;
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
#if !(CNS_SERVER_MULTIPLE && CNS_SYNC_HOST)
        else
        {
            UpdateLobby(lobbySettings);
        }
#endif

#if CNS_SERVER_MULTIPLE && CNS_SYNC_HOST
        if (NetResources.Instance.GameMode != GameMode.Singleplayer)
        {
            StartCoroutine(webAPI.UpdateLobbyCoroutine(lobbySettings, CurrentLobby.CurrentUser.Settings, (userGuid, settings) =>
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

    public void JoinExistingLobby(int lobbyId, ServerSettings serverSettings = null, bool invokeEvent = true)
    {
        if (NetResources.Instance.GameMode == GameMode.Singleplayer)
        {
            JoinLobby(lobbyId, NetResources.Instance.DefaultLobbySettings, serverSettings, invokeEvent);
            return;
        }

#if CNS_SERVER_MULTIPLE
        StartCoroutine(webAPI.JoinLobbyCoroutine(lobbyId, CurrentLobby.CurrentUser.Settings, (userGuid, settings) =>
        {
            // User recreated
            CreateUser(userGuid, settings, false);
        }, (joinedLobbyId, lobbySettingsResponse, serverSettingsResponse) =>
        {
            // Lobby joined
            JoinLobby(joinedLobbyId, lobbySettingsResponse, serverSettingsResponse, invokeEvent);
        }));
#elif CNS_SERVER_SINGLE
        JoinLobby(lobbyId, NetResources.Instance.DefaultLobbySettings, serverSettings, invokeEvent);
#endif
    }

#nullable enable
    private void JoinLobby(int lobbyId, LobbySettings lobbySettings, ServerSettings? serverSettings, bool invokeEvent)
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
        CurrentServerSettings = serverSettings;
        if (invokeEvent)
        {
            OnLobbyJoinRequested?.Invoke(lobbyId, serverSettings);
        }
    }
#nullable disable

    public void Shutdown()
    {
        transportUtility.RemoveTransport();
    }

    public void RegisterTransport(TransportType transportType)
    {
        transportUtility.RegisterTransport(transportType, NetDeviceType.Client);
    }

    public void SetTransport(NetTransport newTransport)
    {
        transportUtility.SetTransport(newTransport);
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

    public void RegisterUnconnectedService<T>(T service) where T : ClientService
    {
        if (unconnectedServices.RegisterService(service))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Registered unconnected ClientService {typeof(T)}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ClientService {typeof(T)} is already registered.");
        }
    }

    public void UnregisterUnconnectedService<T>() where T : ClientService
    {
        if (unconnectedServices.UnregisterService<T>())
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Unregistered unconnected ClientService {typeof(T)}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ClientService {typeof(T)} is not registered.");
        }
    }

    public T GetUnconnectedService<T>() where T : ClientService
    {
        ClientService service = unconnectedServices.GetService<T>();
        if (service != null)
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ClientService {typeof(T)} not found.");
            return null;
        }
    }
}
