#if CNS_DATABASE_ACCESS
using StackExchange.Redis;
using System.Threading.Tasks;
using System;
using System.Threading;

public class ServerDatabaseHandler
{
    private ConnectionMultiplexer redis;
    private IDatabase db;
    private CancellationTokenSource heartbeatTokenSource;

    private Guid gameServerId;

    public async Task Connect(string connectionString, Guid gameServerId)
    {
        redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
        db = redis.GetDatabase();
        this.gameServerId = gameServerId;
    }

    public void StartHeartbeat(int secondsBetweenHeartbeats)
    {
        heartbeatTokenSource = new CancellationTokenSource();
        var token = heartbeatTokenSource.Token;


        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await SetGameServerHeartbeatAsync();
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
        await RemoveGameServerAsync();
    }

    /* GAME SERVER */

    public async Task SaveGameServerMetadataAsync(GameServerData gameServerData)
    {
        await db.HashSetAsync($"game_server:{gameServerData.GameServerId}", new HashEntry[]
            {
            new HashEntry("game_server_id", gameServerData.GameServerId.ToString()),
            new HashEntry("game_server_key", gameServerData.GameServerKey),
            new HashEntry("game_server_address", gameServerData.GameServerAddress),
            });
        await db.SetAddAsync("game_servers", gameServerData.GameServerId.ToString());

        string usersKey = $"lobby:{gameServerData.GameServerId}:connected_users";
        await db.KeyDeleteAsync(usersKey);
        string lobbiesKey = $"lobby:{gameServerData.GameServerId}:active_lobbies";
        await db.KeyDeleteAsync(lobbiesKey);
    }

    public async Task AddUserToGameServerAsync(Guid userGlobalId)
    {
        string usersKey = $"game_server:{gameServerId}:connected_users";
        await db.SetAddAsync(usersKey, userGlobalId.ToString());
    }

    public async Task RemoveUserFromGameServerAsync(Guid userGlobalId)
    {
        string usersKey = $"game_server:{gameServerId}:connected_users";
        await db.SetRemoveAsync(usersKey, userGlobalId.ToString());
    }

    public async Task AddLobbyToGameServerAsync(int lobbyId)
    {
        string lobbiesKey = $"game_server:{gameServerId}:active_lobbies";
        await db.SetAddAsync(lobbiesKey, lobbyId.ToString());
    }

    public async Task RemoveLobbyFromGameServerAsync(int lobbyId)
    {
        string lobbiesKey = $"game_server:{gameServerId}:active_lobbies";
        await db.SetRemoveAsync(lobbiesKey, lobbyId.ToString());
    }

    public async Task SetGameServerHeartbeatAsync()
    {
        await db.StringSetAsync($"game_server:{gameServerId}:heartbeat", "alive", TimeSpan.FromSeconds(30));
    }

    public async Task RemoveGameServerAsync()
    {
        await db.KeyDeleteAsync($"game_server:{gameServerId}");
        await db.KeyDeleteAsync($"game_server:{gameServerId}:heartbeat");
        await db.SetRemoveAsync("game_servers", gameServerId.ToString());
    }

    /* LOBBY */

    public async Task SaveLobbyMetadataAsync(LobbyData lobby)
    {
        await db.HashSetAsync($"lobby:{lobby.LobbyId}", new HashEntry[]
        {
            new HashEntry("lobby_id", lobby.LobbyId),
            new HashEntry("game_server_id", gameServerId.ToString()),
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
            new HashEntry("game_server_id", gameServerId.ToString()),
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
