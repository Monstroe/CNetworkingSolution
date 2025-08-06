#if CNS_TOKEN_VERIFIER
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
                        NetworkManager.Instance.DisconnectUser(user);
                    }
                }
            }
        });
    }

    public void StartTokenCleanup(int tokenValidityDurationMinutes)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                var now = DateTime.UtcNow;

                foreach ((Guid tokenId, DateTime expirationTime) in verifiedUserWithNonExpiredTokens)
                {
                    if ((now - expirationTime) > TimeSpan.FromMinutes(tokenValidityDurationMinutes))
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

    public ServerConnectionData VerifyToken(string token)
    {
        try
        {
            IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtValidator validator = new JwtValidator(serializer, new UtcDateTimeProvider());

            var decoder = new JwtDecoder(serializer, validator, urlEncoder, algorithm);
            var payload = decoder.DecodeToObject<Dictionary<string, object>>(token, secretKey, verify: true);

            ServerConnectionData connectionData = new ServerConnectionData
            {
                TokenId = GetTokenIdFromToken(payload),
                UserGuid = GetUserGuidFromToken(payload),
                UserName = GetUserNameFromToken(payload),
                LobbyId = GetLobbyIdFromToken(payload)
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
            Debug.LogError($"<color=red><b>CNS</b></color>: JWT verification error: {ex.Message}");
        }

        return null;
    }

    private Guid GetTokenIdFromToken(Dictionary<string, object> payload)
    {
        return Guid.Parse(payload["token_id"].ToString());
    }

    private int GetLobbyIdFromToken(Dictionary<string, object> payload)
    {
        return Convert.ToInt32(payload["lobby_id"]);
    }

    private Guid GetUserGuidFromToken(Dictionary<string, object> payload)
    {
        return Guid.Parse(payload["user_guid"].ToString());
    }

    private string GetUserNameFromToken(Dictionary<string, object> payload)
    {
        return payload["user_name"].ToString();
    }
}
#endif
