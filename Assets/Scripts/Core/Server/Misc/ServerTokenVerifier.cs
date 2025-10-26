#if CNS_SERVER_MULTIPLE && CNS_SYNC_DEDICATED
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using System.Collections.Generic;
using JWT.Exceptions;
using System;
using UnityEngine;
using System.Threading.Tasks;

public class ServerTokenVerifier
{
    private readonly string secretKey;

    private Dictionary<UserData, DateTime> unverifiedUsers = new Dictionary<UserData, DateTime>();
    private Dictionary<Guid, DateTime> verifiedUserWithNonExpiredTokens = new Dictionary<Guid, DateTime>();

    public ServerTokenVerifier(string Key)
    {
        secretKey = Key;
    }

    public void StartUnverifiedUserCleanup(int maxSecondsBeforeUnverifiedUserRemoval)
    {
        Debug.Log("<color=green><b>CNS</b></color>: Starting unverified user cleanup.");
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                var now = DateTime.UtcNow;

                foreach ((UserData user, DateTime timestamp) in unverifiedUsers)
                {
                    if ((now - timestamp) > TimeSpan.FromSeconds(maxSecondsBeforeUnverifiedUserRemoval))
                    {
                        Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Removing unverified user {user.UserId} after timeout.");
                        unverifiedUsers.Remove(user);
                        ServerManager.Instance.KickUser(user);
                    }
                }
            }
        });
    }

    public void StartTokenCleanup(int tokenValidityDurationSeconds, int checkIntervalSeconds)
    {
        Debug.Log("<color=green><b>CNS</b></color>: Starting token cleanup.");
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(checkIntervalSeconds));
                var now = DateTime.UtcNow;

                foreach ((Guid tokenId, DateTime expirationTime) in verifiedUserWithNonExpiredTokens)
                {
                    if ((now - expirationTime) > TimeSpan.FromSeconds(tokenValidityDurationSeconds))
                    {
                        verifiedUserWithNonExpiredTokens.Remove(tokenId);
                    }
                }
            }
        });
    }

    public void AddUnverifiedUser(UserData user)
    {
        unverifiedUsers[user] = DateTime.UtcNow;
    }

    public void RemoveUnverifiedUser(UserData user)
    {
        if (unverifiedUsers.ContainsKey(user))
        {
            unverifiedUsers.Remove(user);
        }
    }

    public ConnectionData VerifyToken(string token)
    {
        try
        {
            IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtValidator validator = new JwtValidator(serializer, new UtcDateTimeProvider());

            var decoder = new JwtDecoder(serializer, validator, urlEncoder, algorithm);
            var payload = decoder.DecodeToObject<Dictionary<string, object>>(token, secretKey, verify: true);

            ConnectionData connectionData = new ConnectionData
            {
                TokenId = GetTokenIdFromToken(payload),
                LobbyId = GetLobbyIdFromToken(payload),
                LobbyConnectionType = GetLobbyConnectionTypeFromToken(payload),
                UserGuid = GetUserGuidFromToken(payload),
                UserSettings = GetUserSettingsFromToken(payload),
                LobbySettings = GetLobbySettingsFromToken(payload)
            };

            if (verifiedUserWithNonExpiredTokens.ContainsKey(connectionData.TokenId))
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: User {connectionData.UserGuid} attempted to send an already verified token.");
                return null;
            }

            verifiedUserWithNonExpiredTokens[connectionData.TokenId] = DateTime.UtcNow;

            return connectionData;
        }
        catch (TokenExpiredException) { }
        catch (SignatureVerificationException) { }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red><b>CNS</b></color>: Token verification error: {ex.Message}");
        }

        return null;
    }

    private Guid GetTokenIdFromToken(Dictionary<string, object> payload)
    {
        if (payload.TryGetValue("token_id", out object tokenIdObj))
        {
            return Guid.Parse(tokenIdObj.ToString());
        }
        throw new Exception("<color=red><b>CNS</b></color>: Token does not contain 'token_id' field.");
    }

    private int GetLobbyIdFromToken(Dictionary<string, object> payload)
    {
        if (payload.TryGetValue("lobby_id", out object lobbyIdObj))
        {
            return Convert.ToInt32(lobbyIdObj);
        }
        throw new Exception("<color=red><b>CNS</b></color>: Token does not contain 'lobby_id' field.");
    }

    private LobbyConnectionType GetLobbyConnectionTypeFromToken(Dictionary<string, object> payload)
    {
        if (payload.TryGetValue("lobby_connection_type", out object lobbyConnectionTypeObj))
        {
            return Enum.Parse<LobbyConnectionType>(lobbyConnectionTypeObj.ToString());
        }
        throw new Exception("<color=red><b>CNS</b></color>: Token does not contain 'lobby_connection_type' field.");
    }

    private Guid GetUserGuidFromToken(Dictionary<string, object> payload)
    {
        if (payload.TryGetValue("user_guid", out object userGuidObj))
        {
            return Guid.Parse(userGuidObj.ToString());
        }
        throw new Exception("<color=red><b>CNS</b></color>: Token does not contain 'user_guid' field.");
    }

    private UserSettings GetUserSettingsFromToken(Dictionary<string, object> payload)
    {
        UserSettings settings = new UserSettings();

        if (payload.TryGetValue("user_name", out object userNameObj))
        {
            settings.UserName = userNameObj.ToString();
        }
        else
        {
            throw new Exception("<color=red><b>CNS</b></color>: Token does not contain 'user_name' field.");
        }

        return settings;
    }

    private LobbySettings GetLobbySettingsFromToken(Dictionary<string, object> payload)
    {
        LobbySettings settings = new LobbySettings();

#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
        if (payload.TryGetValue("steam_code", out object steamCodeObj))
        {
            settings.SteamCode = Convert.ToUInt64(steamCodeObj);
        }
        else
        {
            throw new Exception("<color=red><b>CNS</b></color>: Token does not contain 'steam_code' field.");
        }
#endif

        if (payload.TryGetValue("max_users", out object maxUsersObj))
        {
            settings.MaxUsers = Convert.ToInt32(maxUsersObj);
        }
        else
        {
            throw new Exception("<color=red><b>CNS</b></color>: Token does not contain 'max_users' field.");
        }

        if (payload.TryGetValue("lobby_visibility", out object lobbyVisibilityObj))
        {
            settings.LobbyVisibility = Enum.Parse<LobbyVisibility>(lobbyVisibilityObj.ToString());
        }
        else
        {
            throw new Exception("<color=red><b>CNS</b></color>: Token does not contain 'lobby_visibility' field.");
        }

        if (payload.TryGetValue("lobby_name", out object lobbyNameObj))
        {
            settings.LobbyName = lobbyNameObj.ToString();
        }
        else
        {
            throw new Exception("<color=red><b>CNS</b></color>: Token does not contain 'lobby_name' field.");
        }

        return settings;
    }
}
#endif
