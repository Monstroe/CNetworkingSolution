#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public class SteamRelayTransport : NetTransport, IConnectionManager, ISocketManager
{
    [Tooltip("The Steam App ID of your game. Technically you're not allowed to use 480, but Valve doesn't do anything about it so it's fine for testing purposes.")]
    [SerializeField] private uint steamAppId = 480;
    [Tooltip("The Steam ID of the user targeted when joining as a client.")]
    [SerializeField] private ulong targetSteamId;
    [Tooltip("When in play mode, this will display your Steam ID.")]
    [SerializeField] private ulong userSteamId;
    [Tooltip("Size of default buffer for decoding incoming packets, in bytes")]
    [SerializeField] private int messageBufferSize = 1024 * 4;

    // Starts on Client
    private ConnectionManager connectionManager;
    // Starts on Server
    private SocketManager socketManager;
    private Dictionary<uint, Client> connectedClients;
    private byte[] messageBuffer;

    public override uint ServerClientId => 0;
    public override List<uint> ConnectedClientIds => new List<uint>(connectedClients.Keys);

    private class Client
    {
        public SteamId steamId;
        public Connection connection;
    }

    void FixedUpdate()
    {
        SteamClient.RunCallbacks();
        connectionManager?.Receive();
        socketManager?.Receive();
    }

    void OnDestroy()
    {
        if (!initialized)
        {
            Shutdown();
        }

        SteamClient.Shutdown();
    }

    public override void Initialize(NetDeviceType deviceType)
    {
        this.deviceType = deviceType;

        try
        {
            SteamClient.Init(steamAppId, false);
            Debug.Log("<color=green><b>CNS</b></color>: Initialized Steam Client.");
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Exception occurred while initializing Steam: " + e.Message);
        }
        finally
        {
            StartCoroutine(InitSteamworks());
        }

        messageBuffer = new byte[messageBufferSize];
        connectedClients = new Dictionary<uint, Client>();

        SteamFriends.OnGameLobbyJoinRequested += async (lobby, friend) =>
        {
            await JoinSteamLobby(lobby.Id);
        };

#nullable enable
        if (deviceType == NetDeviceType.Client)
        {
            ClientManager.Instance.OnLobbyCreateRequested += async (serverSettings) =>
            {
                await CreateSteamLobby(ClientManager.Instance.CurrentLobby.Settings);
            };

            ClientManager.Instance.OnLobbyJoinRequested += async (lobbyId, serverSettings) =>
            {
                await JoinSteamLobby(ClientManager.Instance.CurrentLobby.Settings.SteamCode);
            };
        }
#nullable disable
    }

    private async Task CreateSteamLobby(LobbySettings lobbySettings)
    {
        Lobby? lobby = await SteamMatchmaking.CreateLobbyAsync(lobbySettings.MaxUsers);
        SteamId lobbyId = lobby?.Id ?? 0;
        if (lobbyId == 0)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Failed to create Steam lobby.");
            return;
        }

        SteamFriends.SetRichPresence("connect", lobbyId.ToString());
        lobbySettings.SteamCode = lobbyId;
        ClientManager.Instance.UpdateCurrentLobby(lobbySettings);

        StartServer();
    }

    private async Task JoinSteamLobby(ulong lobbyCode)
    {
        Lobby? lobby = await SteamMatchmaking.JoinLobbyAsync(lobbyCode);
        targetSteamId = lobby?.Owner.Id ?? 0;
        if (targetSteamId == 0)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Failed to join Steam lobby.");
            return;
        }

        StartClient();
    }

    protected override bool StartClient()
    {
        if (initialized)
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Already started as " + deviceType);
            return false;
        }

        initialized = true;

        connectionManager = SteamNetworkingSockets.ConnectRelay<ConnectionManager>(targetSteamId);
        connectionManager.Interface = this;
        return true;
    }

    protected override bool StartServer()
    {
        if (initialized)
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Already started as " + deviceType);
            return false;
        }

        initialized = true;

        socketManager = SteamNetworkingSockets.CreateRelaySocket<SocketManager>();
        socketManager.Interface = this;

        targetSteamId = SteamClient.SteamId;

        return true;
    }


    public override void Disconnect()
    {
        connectionManager?.Close();
        socketManager?.Close();
        connectedClients.Clear();
    }

    public override void DisconnectRemote(uint remoteId)
    {
        if (connectedClients.TryGetValue(remoteId, out Client user))
        {
            // Flush any pending messages before closing the connection
            user.connection.Flush();
            user.connection.Close();
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to disconnect a client that is not connected: {remoteId}");
        }
    }

    private SendType ConvertProtocol(TransportMethod method)
    {
        switch (method)
        {
            case TransportMethod.Unreliable:
                {
                    return SendType.Unreliable;
                }
            case TransportMethod.UnreliableSequenced:
                {
                    Debug.LogWarning("<color=yellow><b>CNS</b></color>: UnreliableSequenced is not supported by Steamworks. Falling back to Reliable.");
                    return SendType.Reliable;
                }
            case TransportMethod.Reliable:
                {
                    return SendType.Reliable;
                }
            case TransportMethod.ReliableUnordered:
                {
                    Debug.LogWarning("<color=yellow><b>CNS</b></color>: ReliableUnordered is not supported by Steamworks. Falling back to Reliable.");
                    return SendType.Reliable;
                }
            default:
                {
                    throw new ArgumentOutOfRangeException("<color=red><b>CNS</b></color>: Unknown protocol: " + method);
                }
        }
    }

    private TransportMethod ConvertProtocolBack(SendType method)
    {
        switch (method)
        {
            case SendType.Unreliable:
                {
                    return TransportMethod.Unreliable;
                }
            case SendType.Reliable:
                {
                    return TransportMethod.Reliable;
                }
            default:
                {
                    throw new ArgumentOutOfRangeException("<color=red><b>CNS</b></color>: Unknown protocol: " + method);
                }
        }
    }

    public override void Shutdown()
    {
        try
        {
            Disconnect();
            initialized = false;
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>CNS</b></color>: Exception occurred while shutting down: " + e.Message);
        }
    }

    public override void Send(uint remoteId, NetPacket packet, TransportMethod method)
    {
        var sendType = ConvertProtocol(method);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(packet.Length);
        packet.CopyTo(0, buffer, 0, packet.Length);

        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);

            if (remoteId == ServerClientId)
            {
                connectionManager.Connection.SendMessage(ptr, packet.Length, sendType);
            }
            else if (connectedClients.TryGetValue(remoteId, out Client user))
            {
                user.connection.SendMessage(ptr, packet.Length, sendType);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to send to a client that is not connected: {remoteId}");
            }
        }
        finally
        {
            handle.Free();
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public override void SendToList(List<uint> remoteIds, NetPacket packet, TransportMethod method)
    {
        var sendType = ConvertProtocol(method);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(packet.Length);
        packet.CopyTo(0, buffer, 0, packet.Length);

        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);

            foreach (var remoteId in remoteIds)
            {
                if (remoteId == ServerClientId)
                {
                    connectionManager.Connection.SendMessage(ptr, packet.Length, sendType);
                }
                else if (connectedClients.TryGetValue(remoteId, out Client user))
                {
                    user.connection.SendMessage(ptr, packet.Length, sendType);
                }
                else
                {
                    Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to send to a client that is not connected: {remoteId}");
                }
            }
        }
        finally
        {
            handle.Free();
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public override void SendToAll(NetPacket packet, TransportMethod method)
    {
        var sendType = ConvertProtocol(method);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(packet.Length);
        packet.CopyTo(0, buffer, 0, packet.Length);

        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);

            if (deviceType == NetDeviceType.Client)
            {
                connectionManager.Connection.SendMessage(ptr, packet.Length, sendType);
            }
            else
            {
                foreach (var user in connectedClients.Values)
                {
                    user.connection.SendMessage(ptr, packet.Length, sendType);
                }
            }
        }
        finally
        {
            handle.Free();
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /* CLIENT EVENTS */
    void IConnectionManager.OnConnecting(ConnectionInfo info)
    {
        // Ignore
    }

    void IConnectionManager.OnConnected(ConnectionInfo info)
    {
        RaiseNetworkConnected(ServerClientId);
    }

    void IConnectionManager.OnDisconnected(ConnectionInfo info)
    {
        RaiseNetworkDisconnected(ServerClientId);
    }

    unsafe void IConnectionManager.OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        EnsureBufferSize(size);

        fixed (byte* payload = messageBuffer)
        {
            UnsafeUtility.MemCpy(payload, (byte*)data, size);
        }

        RaiseNetworkReceived(ServerClientId, new NetPacket(new ArraySegment<byte>(messageBuffer, 0, size)), null);
    }

    /* SERVER EVENTS */
    void ISocketManager.OnConnecting(Connection connection, ConnectionInfo info)
    {
        connection.Accept();
    }

    void ISocketManager.OnConnected(Connection connection, ConnectionInfo info)
    {
        if (!connectedClients.ContainsKey(connection.Id))
        {
            connectedClients.Add(connection.Id, new Client { steamId = info.Identity.SteamId, connection = connection });
            RaiseNetworkConnected(connection.Id);
        }
        else
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Attempting to connect a client that is already connected: " + connection.Id);
        }
    }

    void ISocketManager.OnDisconnected(Connection connection, ConnectionInfo info)
    {
        if (connectedClients.Remove(connection.Id))
        {
            RaiseNetworkDisconnected(connection.Id);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Attempting to disconnect a client that is not connected: {connection.Id}");
        }
    }

    unsafe void ISocketManager.OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        EnsureBufferSize(size);

        fixed (byte* payload = messageBuffer)
        {
            UnsafeUtility.MemCpy(payload, (byte*)data, size);
        }

        RaiseNetworkReceived(connection.Id, new NetPacket(new ArraySegment<byte>(messageBuffer, 0, size)), null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureBufferSize(int size)
    {
        if (messageBuffer.Length >= size)
        {
            return;
        }

        messageBuffer = new byte[Math.Max(messageBuffer.Length * 2, size)];
    }

    private IEnumerator InitSteamworks()
    {
        yield return new WaitUntil(() => SteamClient.IsValid);

        SteamNetworkingUtils.InitRelayNetworkAccess();

        Debug.Log("<color=green><b>CNS</b></color>: Initialized access to Steam Relay Network.");

        userSteamId = SteamClient.SteamId;

        Debug.Log("<color=green><b>CNS</b></color>: Fetched user Steam ID.");
    }
}
#endif
