#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
using StackExchange.Redis;
using System.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

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
        Debug.Log($"<color=green><b>CNS</b></color>: Connected to Redis database at {connectionString}.");
    }

    public void StartHeartbeat(int secondsBetweenHeartbeats)
    {
        heartbeatTokenSource = new CancellationTokenSource();
        var token = heartbeatTokenSource.Token;


        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await SetServerHeartbeatAsync(secondsBetweenHeartbeats * 2);
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

        if (redis != null)
        {
            await RemoveServerAsync();
            foreach (var lobbyId in ServerManager.Instance.ServerData.ActiveLobbies.Keys)
            {
                await DeleteLobbyAsync(lobbyId);
            }
            foreach (var user in ServerManager.Instance.ServerData.ConnectedUsers.Values)
            {
                await DeleteUserAsync(user.GlobalGuid);
            }
            redis.Close();
            redis.Dispose();
            Debug.Log($"<color=green><b>CNS</b></color>: Disconnected from Redis database.");
        }
    }

    /* SERVER */

    public async Task SaveServerMetadataAsync(ServerData serverData, bool isNew = true)
    {
        await db.HashSetAsync($"game_server:{serverData.Settings.ServerId}", new HashEntry[]
        {
            new HashEntry("game_server_id", serverData.Settings.ServerId.ToString()),
            new HashEntry("game_server_key", serverData.Settings.ServerKey),
            new HashEntry("game_server_address", serverData.Settings.ServerAddress),
        });
        await db.SetAddAsync("game_servers", serverData.Settings.ServerId.ToString());

        if (isNew)
        {
            string usersKey = $"game_server:{serverData.Settings.ServerId}:connected_users";
            await db.KeyDeleteAsync(usersKey);
            foreach (var user in serverData.ConnectedUsers.Values)
            {
                if (user.GlobalGuid != Guid.Empty)
                {
                    await db.SetAddAsync(usersKey, user.GlobalGuid.ToString());
                }
            }

            string lobbiesKey = $"game_server:{serverData.Settings.ServerId}:active_lobbies";
            await db.KeyDeleteAsync(lobbiesKey);
            foreach (var lobby in serverData.ActiveLobbies.Values)
            {
                await db.SetAddAsync(lobbiesKey, lobby.LobbyData.LobbyId.ToString());
            }
        }

        Debug.Log($"<color=green><b>CNS</b></color>: Saved server {serverData.Settings.ServerId}.");
    }

    public async Task AddUserToServerAsync(Guid userGlobalId)
    {
        string usersKey = $"game_server:{serverId}:connected_users";
        await db.SetAddAsync(usersKey, userGlobalId.ToString());
        Debug.Log($"<color=green><b>CNS</b></color>: Added user {userGlobalId} to server.");
    }

    public async Task AddUserToServerLimboAsync(ulong userId, int maxSecondsBeforeUnverifiedUserRemoval)
    {
        string limboKey = $"game_server:{serverId}:user_limbo";
        await db.SetAddAsync(limboKey, userId.ToString());
        await db.KeyExpireAsync(limboKey, TimeSpan.FromSeconds(maxSecondsBeforeUnverifiedUserRemoval));
        Debug.Log($"<color=green><b>CNS</b></color>: Added user {userId} to server limbo.");
    }

    public async Task RemoveUserFromServerAsync(Guid userGlobalId)
    {
        string usersKey = $"game_server:{serverId}:connected_users";
        bool removed = await db.SetRemoveAsync(usersKey, userGlobalId.ToString());
        if (removed)
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Removed user {userGlobalId} from server.");
        }
    }

    public async Task RemoveUserFromServerLimboAsync(ulong userId)
    {
        string limboKey = $"game_server:{serverId}:user_limbo";
        bool removed = await db.SetRemoveAsync(limboKey, userId.ToString());
        if (removed)
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Removed user {userId} from server limbo.");
        }
    }

    public async Task AddLobbyToServerAsync(int lobbyId)
    {
        string lobbiesKey = $"game_server:{serverId}:active_lobbies";
        await db.SetAddAsync(lobbiesKey, lobbyId.ToString());
        Debug.Log($"<color=green><b>CNS</b></color>: Added lobby {lobbyId} to server.");
    }

    public async Task RemoveLobbyFromServerAsync(int lobbyId)
    {
        string lobbiesKey = $"game_server:{serverId}:active_lobbies";
        await db.SetRemoveAsync(lobbiesKey, lobbyId.ToString());
        Debug.Log($"<color=green><b>CNS</b></color>: Removed lobby {lobbyId} from server.");
    }

    public async Task SetServerHeartbeatAsync(double heartbeatIntervalSeconds)
    {
        await db.StringSetAsync($"game_server:{serverId}:heartbeat", "alive", TimeSpan.FromSeconds(heartbeatIntervalSeconds));
        Debug.Log($"<color=green><b>CNS</b></color>: Heartbeat sent for server {serverId}.");
    }

    public async Task RemoveServerAsync()
    {
        await db.KeyDeleteAsync($"game_server:{serverId}");
        await db.KeyDeleteAsync($"game_server:{serverId}:heartbeat");
        await db.KeyDeleteAsync($"game_server:{serverId}:active_lobbies");
        await db.KeyDeleteAsync($"game_server:{serverId}:connected_users");
        await db.KeyDeleteAsync($"game_server:{serverId}:user_limbo");
        await db.SetRemoveAsync("game_servers", serverId.ToString());
        Debug.Log($"<color=green><b>CNS</b></color>: Removed server {serverId} from database.");
    }

    /* LOBBY */

    public async Task SaveLobbyMetadataAsync(LobbyData lobby, bool isNew = true)
    {
        await db.HashSetAsync($"lobby:{lobby.LobbyId}", new HashEntry[]
        {
            new HashEntry("lobby_id", lobby.LobbyId),
            new HashEntry("game_server_id", serverId.ToString()),
            new HashEntry("max_users", lobby.Settings.MaxUsers),
            new HashEntry("lobby_visibility", lobby.Settings.LobbyVisibility.ToString()),
            new HashEntry("lobby_name", lobby.Settings.LobbyName),
        });

        if (isNew)
        {
            string usersKey = $"lobby:{lobby.LobbyId}:users";
            await db.KeyDeleteAsync(usersKey);
            foreach (var user in lobby.LobbyUsers)
            {
                await db.ListRightPushAsync(usersKey, user.ToString());
            }
        }

        Debug.Log($"<color=green><b>CNS</b></color>: Saved lobby {lobby.LobbyId}.");
    }

    public async void UpdateLobbyMetadataAsync(LobbyData lobby)
    {
        await SaveLobbyMetadataAsync(lobby, false);
    }

    public async Task RemoveLobbyFromLimbo(int lobbyId)
    {
        await db.SetRemoveAsync("lobby_limbo", lobbyId);
        Debug.Log($"<color=green><b>CNS</b></color>: Removed lobby {lobbyId} from limbo.");
    }

    public async Task DeleteLobbyAsync(int lobbyId)
    {
        var keys = new RedisKey[]
        {
            $"lobby:{lobbyId}",
            $"lobby:{lobbyId}:users"
        };
        await db.KeyDeleteAsync(keys);
        Debug.Log($"<color=green><b>CNS</b></color>: Deleted lobby {lobbyId} from database.");
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
        Debug.Log($"<color=green><b>CNS</b></color>: Saved user {user.GlobalGuid}.");
    }

    public async void UpdateUserMetadataAsync(UserData user)
    {
        await SaveUserMetadataAsync(user);
    }

    public async Task AddUserToLobbyAsync(int lobbyId, Guid userGuid)
    {
        await db.ListRightPushAsync($"lobby:{lobbyId}:users", userGuid.ToString());
        Debug.Log($"<color=green><b>CNS</b></color>: Added user {userGuid} to lobby {lobbyId}.");
    }

    public async Task RemoveUserFromLobbyAsync(int lobbyId, Guid userGuid)
    {
        await db.ListRemoveAsync($"lobby:{lobbyId}:users", userGuid.ToString());
        Debug.Log($"<color=green><b>CNS</b></color>: Removed user {userGuid} from lobby {lobbyId}.");
    }

    public async Task DeleteUserAsync(Guid userGuid)
    {
        bool deleted = await db.KeyDeleteAsync($"user:{userGuid}");
        if (deleted)
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Deleted user {userGuid} from database.");
        }
    }
}
#endif
