#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
using StackExchange.Redis;
using System.Threading.Tasks;
using System;
using System.Threading;

public class ServerDatabaseHandler
{
    private ConnectionMultiplexer redis;
    private IDatabase db;
    private CancellationTokenSource heartbeatTokenSource;

    private Guid serverId;

    public async Task Connect(string connectionString, Guid serverId)
    {
        redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
        db = redis.GetDatabase();
        this.serverId = serverId;
    }

    public void StartHeartbeat(int secondsBetweenHeartbeats)
    {
        heartbeatTokenSource = new CancellationTokenSource();
        var token = heartbeatTokenSource.Token;


        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await SetServerHeartbeatAsync();
                await Task.Delay(TimeSpan.FromSeconds(secondsBetweenHeartbeats), token);
            }
        });
    }

    public async Task Close()
    {
        if (heartbeatTokenSource != null)
        {
            heartbeatTokenSource.Cancel();
            heartbeatTokenSource.Dispose();
        }
        await RemoveServerAsync();
    }

    /* SERVER */

    public async Task SaveServerMetadataAsync(ServerData serverData)
    {
        await db.HashSetAsync($"game_server:{serverData.Settings.ServerId}", new HashEntry[]
            {
            new HashEntry("game_server_id", serverData.Settings.ServerId.ToString()),
            new HashEntry("game_server_key", serverData.Settings.ServerKey),
            new HashEntry("game_server_address", serverData.Settings.ServerAddress),
            });
        await db.SetAddAsync("game_servers", serverData.Settings.ServerId.ToString());

        string usersKey = $"lobby:{serverData.Settings.ServerId}:connected_users";
        await db.KeyDeleteAsync(usersKey);
        string lobbiesKey = $"lobby:{serverData.Settings.ServerId}:active_lobbies";
        await db.KeyDeleteAsync(lobbiesKey);
    }

    public async Task AddUserToServerAsync(Guid userGlobalId)
    {
        string usersKey = $"game_server:{serverId}:connected_users";
        await db.SetAddAsync(usersKey, userGlobalId.ToString());
    }

    public async Task RemoveUserFromServerAsync(Guid userGlobalId)
    {
        string usersKey = $"game_server:{serverId}:connected_users";
        await db.SetRemoveAsync(usersKey, userGlobalId.ToString());
    }

    public async Task AddLobbyToServerAsync(int lobbyId)
    {
        string lobbiesKey = $"game_server:{serverId}:active_lobbies";
        await db.SetAddAsync(lobbiesKey, lobbyId.ToString());
    }

    public async Task RemoveLobbyFromServerAsync(int lobbyId)
    {
        string lobbiesKey = $"game_server:{serverId}:active_lobbies";
        await db.SetRemoveAsync(lobbiesKey, lobbyId.ToString());
    }

    public async Task SetServerHeartbeatAsync()
    {
        await db.StringSetAsync($"game_server:{serverId}:heartbeat", "alive", TimeSpan.FromSeconds(30));
    }

    public async Task RemoveServerAsync()
    {
        await db.KeyDeleteAsync($"game_server:{serverId}");
        await db.KeyDeleteAsync($"game_server:{serverId}:heartbeat");
        await db.SetRemoveAsync("game_servers", serverId.ToString());
    }

    /* LOBBY */

    public async Task SaveLobbyMetadataAsync(LobbyData lobby)
    {
        await db.HashSetAsync($"lobby:{lobby.LobbyId}", new HashEntry[]
        {
            new HashEntry("lobby_id", lobby.LobbyId),
            new HashEntry("game_server_id", serverId.ToString()),
            new HashEntry("max_users", lobby.Settings.MaxUsers),
            new HashEntry("lobby_visibility", lobby.Settings.LobbyVisibility.ToString()),
            new HashEntry("lobby_name", lobby.Settings.LobbyName),
        });

        string usersKey = $"lobby:{lobby.LobbyId}:users";
        await db.KeyDeleteAsync(usersKey);
        foreach (var user in lobby.LobbyUsers)
        {
            await db.ListRightPushAsync(usersKey, user.ToString());
        }
    }

    public async Task RemoveLobbyFromLimbo(int lobbyId)
    {
        await db.SetRemoveAsync("lobby_limbo", lobbyId);
    }

    public async Task DeleteLobbyAsync(int lobbyId)
    {
        var keys = new RedisKey[]
        {
            $"lobby:{lobbyId}",
            $"lobby:{lobbyId}:users"
        };
        await db.KeyDeleteAsync(keys);
    }

    /* USER */

    public async Task SaveUserMetadataAsync(UserData user)
    {
        await db.HashSetAsync($"user:{user.GlobalGuid}", new HashEntry[]
        {
            new HashEntry("global_guid", user.GlobalGuid.ToString()),
            new HashEntry("lobby_id", user.LobbyId.ToString()),
            new HashEntry("game_server_id", serverId.ToString()),
            new HashEntry("user_name", user.Settings.UserName),
        });
    }

    public async Task AddUserToLobbyAsync(int lobbyId, Guid userGuid)
    {
        await db.ListRightPushAsync($"lobby:{lobbyId}:users", userGuid.ToString());
    }

    public async Task RemoveUserFromLobbyAsync(int lobbyId, Guid userGuid)
    {
        await db.ListRemoveAsync($"lobby:{lobbyId}:users", userGuid.ToString());
    }

    public async Task DeleteUserAsync(Guid userGuid)
    {
        await db.KeyDeleteAsync($"user:{userGuid}");
    }
}
#endif
