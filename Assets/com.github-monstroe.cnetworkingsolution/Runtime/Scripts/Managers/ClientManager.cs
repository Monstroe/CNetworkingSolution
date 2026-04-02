using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

[RequireComponent(typeof(SingleTransportUtility))]
public class ClientManager : MonoBehaviour
{
    public delegate void ConnectionAcceptedEventHandler(ConnectionAcceptedArgs args);
    public event ConnectionAcceptedEventHandler OnConnectionAccepted;

    public delegate void ConnectionRejectedEventHandler(ConnectionRejectedArgs args);
    public event ConnectionRejectedEventHandler OnConnectionRejected;

    public delegate void ConnectionLostEventHandler(ConnectionLostArgs args);
    public event ConnectionLostEventHandler OnConnectionLost;

    public delegate void ConnectionErrorEventHandler(ConnectionErrorArgs args);
    public event ConnectionErrorEventHandler OnConnectionError;

    public static ClientManager Instance { get; private set; }
    public ClientLobby CurrentLobby { get; private set; }
    public bool IsConnected { get; private set; } = false;

    [Header("Lobby Settings")]
    [SerializeField] private ClientLobby lobbyPrefab;

    private SingleTransportUtility transportUtility;
    private ConnectionData connectionData;

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
        CurrentLobby = Instantiate(lobbyPrefab, transform);
        CurrentLobby.Init(transportUtility);
    }

    void Start()
    {
        Debug.Log("<color=green><b>CNS</b></color>: Client initialized.");
    }

    void FixedUpdate()
    {
        if (IsConnected)
        {
            CurrentLobby.Tick();
        }
    }

    void OnDestroy()
    {
        transportUtility.RemoveTransports();
        ClearTransportUtilityEvents();
    }

    private void HandleNetworkConnected(ulong remoteId)
    {
        transportUtility.SendToAllRemotes(ConnectionPacketBuilder.ConnectionRequest(connectionData), TransportMethod.Reliable);
    }

    private void HandleNetworkDisconnected(ulong remoteId, TransportCode code)
    {
        transportUtility.RemoveTransports();
        IsConnected = false;
        CurrentLobby.LobbyData = new LobbyData();
        CurrentLobby.CurrentUser = new UserData();
        CurrentLobby.ClientTick = 0;

        OnConnectionLost?.Invoke(new ConnectionLostArgs()
        {
            Code = code
        });

        Debug.Log($"<color=yellow><b>CNS</b></color>: Client disconnected from lobby.");
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
        else if ((ConnectionCommandType)packet.ReadByte() == ConnectionCommandType.CONNECTION_RESPONSE)
        {
            bool accepted = packet.ReadBool();
            if (accepted)
            {
                int lobbyId = packet.ReadInt();
                NetPacket responsePacket = new NetPacket(packet.ReadBytes());

                CurrentLobby.LobbyData.LobbyId = lobbyId;
                IsConnected = true;

                OnConnectionAccepted?.Invoke(new ConnectionAcceptedArgs()
                {
                    ResponsePacket = responsePacket
                });

                Debug.Log("<color=green><b>CNS</b></color>: Client connected to lobby");
            }
            else
            {
                NetPacket responsePacket = new NetPacket(packet.ReadBytes());

                OnConnectionRejected?.Invoke(new ConnectionRejectedArgs()
                {
                    ResponsePacket = responsePacket
                });

                Debug.LogWarning("<color=yellow><b>CNS</b></color>: Client rejected from lobby");
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Received unknown packet while not in a lobby.");
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
        OnConnectionError?.Invoke(new ConnectionErrorArgs()
        {
            Code = code,
            SocketError = socketError
        });

        Debug.LogError($"<color=red><b>CNS</b></color>: Network error occurred: {code} {(socketError.HasValue ? $"(Socket Error: {socketError.Value})" : "")}");
    }

    public void CreateLobby(NetPacket requestPacket = null)
    {
        connectionData = new ConnectionData()
        {
            LobbyConnectionType = LobbyConnectionType.Create,
            RequestPacket = requestPacket
        };
    }

    public void JoinLobby(int lobbyId, bool createIfNotExists = false, NetPacket requestPacket = null)
    {
        connectionData = new ConnectionData()
        {
            LobbyId = lobbyId,
            LobbyConnectionType = createIfNotExists ? LobbyConnectionType.JoinOrCreate : LobbyConnectionType.JoinIfExists,
            RequestPacket = requestPacket
        };
    }

    public void StartTransport()
    {
        transportUtility.StartTransports();
    }

    public void RegisterTransport<T>() where T : NetTransport
    {
        transportUtility.RegisterTransport<T>(NetDeviceType.Client);
    }

    public void SetTransport(NetTransport newTransport)
    {
        transportUtility.AddTransport(newTransport);
    }

    public void RemoveTransports()
    {
        transportUtility.RemoveTransports();
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

public class ConnectionAcceptedArgs
{
    public NetPacket ResponsePacket { get; internal set; }

    internal ConnectionAcceptedArgs() { }
}

public class ConnectionRejectedArgs
{
    public NetPacket ResponsePacket { get; internal set; }

    internal ConnectionRejectedArgs() { }
}

public class ConnectionLostArgs
{
    public TransportCode Code { get; internal set; }

    internal ConnectionLostArgs() { }
}

public class ConnectionErrorArgs
{
    public TransportCode Code { get; internal set; }
    public SocketError? SocketError { get; internal set; }

    internal ConnectionErrorArgs() { }
}
