using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    public delegate void NetworkConnectedHandler(ConnectedArgs args);
    /// <summary>
    /// Event triggered when the user is connected. This is called when the client connects to the server or when the server has a client connect to it.
    /// </summary>
    public event NetworkConnectedHandler OnNetworkConnected;

    public delegate void NetworkDisconnectedHandler(DisconnectedArgs args);
    /// <summary>
    /// Event triggered when the user is disconnected. This is called when the client disconnects from the server or when the server has a client disconnect from it.
    /// </summary>
    public event NetworkDisconnectedHandler OnNetworkDisconnected;

    public delegate void NetworkReceivedHandler(ReceivedArgs args);
    /// <summary>
    /// Event triggered when a packet is received. This is called when the client receives a packet from the server or when the server receives a packet from a client.
    /// </summary>
    public event NetworkReceivedHandler OnNetworkReceived;

    public AuthorizationType AuthorizationType { get { return authorizationType; } }

    [Tooltip("The transport layer to use for networking.")]
    [SerializeField] private NetTransport transport;
    [Tooltip("The type of authorization to use for the network (dedicated server vs client host)")]
    [SerializeField] private AuthorizationType authorizationType = AuthorizationType.ServerAuthorization;
    [Tooltip("The name of the server scene to load when starting the host (or singleplayer).")]
    [SerializeField] private string serverSceneName = "Server";

    void Awake()
    {
        if (ServerManager.Instance != null)
        {
            ServerManager.Instance.Init(transport);
        }

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: There are multiple NetworkManager instances in the scene, destroying one.");
            Destroy(gameObject);
        }

        if (ClientManager.Instance != null)
        {
            ClientManager.Instance.Init(transport);
        }
    }

    void OnDestroy()
    {
        transport.Shutdown();
    }

    public void StartClient()
    {
        if (transport.StartClient())
        {
            Debug.Log("<color=green><b>CNS</b></color>: Client started successfully.");
        }
        else
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Failed to start client.");
        }
    }

    public void StartServer()
    {
        if (AuthorizationType == AuthorizationType.HostAuthorization)
        {
            SceneManager.LoadScene(serverSceneName, LoadSceneMode.Additive);
        }

        if (transport.StartServer())
        {
            Debug.Log("<color=green><b>CNS</b></color>: Server " + (AuthorizationType == AuthorizationType.HostAuthorization ? "(Host mode) " : "") + "started successfully.");
        }
        else
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Failed to start server.");
        }
    }

    public void StartLocal()
    {
        SceneManager.LoadScene(serverSceneName, LoadSceneMode.Additive);
    }

    public void HandleNetworkConnected(uint remoteId)
    {
        var args = new ConnectedArgs { RemoteId = remoteId };
        OnNetworkConnected?.Invoke(args);
    }

    public void HandleNetworkDisconnected(uint remoteId)
    {
        var args = new DisconnectedArgs { RemoteId = remoteId };
        OnNetworkDisconnected?.Invoke(args);
    }

    public void HandleNetworkReceived(uint remoteId, NetPacket receivedPacket, TransportMethod? method)
    {
        var args = new ReceivedArgs { RemoteId = remoteId, Packet = receivedPacket, TransportMethod = method };
        OnNetworkReceived?.Invoke(args);
    }

    public uint GenerateRandomID()
    {
        return (uint)UnityEngine.Random.Range(1000000000, 9999999999);
    }
}

public class ConnectedArgs
{
    public uint RemoteId { get; set; }
}

public class DisconnectedArgs
{
    public uint RemoteId { get; set; }
}

public class ReceivedArgs
{
    public uint RemoteId { get; set; }
    public NetPacket Packet { get; set; }
    public TransportMethod? TransportMethod { get; set; }
}

public enum AuthorizationType
{
    ServerAuthorization,
    HostAuthorization
}
