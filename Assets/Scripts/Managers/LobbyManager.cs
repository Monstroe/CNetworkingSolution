using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using System;

public class LobbyManager : MonoBehaviour
{
    class UserResponse
    {
        public Guid GlobalGuid { get; set; }
        public UserSettings UserSettings { get; set; }
        public string Token { get; set; }
    }

    class LobbyResponse
    {
        public int LobbyId { get; set; }
        public Guid GameServerId { get; set; }
        public string GameServerAddress { get; set; }
        public LobbySettings LobbySettings { get; set; }
        public string GameServerToken { get; set; }
    }

    public delegate void UserCreatedEventHandler(UserData userData);
    public event UserCreatedEventHandler OnUserCreated;

    public delegate void UserUpdatedEventHandler(UserData userData);
    public event UserUpdatedEventHandler OnUserUpdated;

#if CNS_TOKEN_VERIFIER
    public delegate void LobbyCreatedEventHandler(LobbyData lobbyData, GameServerData gameServerData, string gameServerToken);
#else
    public delegate void LobbyCreatedEventHandler(LobbyData lobbyData, GameServerData gameServerData);
#endif
    public event LobbyCreatedEventHandler OnLobbyCreated;

#if CNS_TOKEN_VERIFIER
    public delegate void LobbyUpdatedEventHandler(LobbyData lobbyData, GameServerData gameServerData, string gameServerToken);
#else
    public delegate void LobbyUpdatedEventHandler(LobbyData lobbyData, GameServerData gameServerData);
#endif
    public event LobbyUpdatedEventHandler OnLobbyUpdated;

#if CNS_TOKEN_VERIFIER
    public delegate void LobbyJoinedEventHandler(LobbyData lobbyData, GameServerData gameServerData, string gameServerToken);
#else
    public delegate void LobbyJoinedEventHandler(LobbyData lobbyData, GameServerData gameServerData);
#endif
    public event LobbyJoinedEventHandler OnLobbyJoined;

    public static LobbyManager Instance { get; private set; }

    public string LobbyApiUrl
    {
        get => lobbyApiUrl;
        set => lobbyApiUrl = value;
    }
    [Tooltip("The URL of the lobby API. PLEASE PUT SLASH AT THE END.")]
    [SerializeField] private string lobbyApiUrl = "http://localhost:8080/api/";

    private UserData cachedUserData;
    private LobbyData cachedLobbyData;
    private GameServerData cachedGameServerData;
    private string webToken;

#if CNS_TOKEN_VERIFIER
    private string gameServerToken;
#endif

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Multiple instances of LobbyManager detected. Destroying duplicate instance.");
            Destroy(gameObject);
        }
    }

    public void CreateUser(UserSettings userSettings = null)
    {
        StartCoroutine(CreateUserCoroutine(userSettings));
    }

    public void UpdateUser(UserSettings userSettings)
    {
        StartCoroutine(UpdateUserCoroutine(userSettings));
    }

    public void CreateLobby(LobbySettings lobbySettings = null)
    {
        StartCoroutine(CreateLobbyCoroutine(lobbySettings));
    }

    public void UpdateLobby(LobbySettings lobbySettings)
    {
        ClientLobby.Instance.SendToRoom(PacketBuilder.LobbySettings(lobbySettings), TransportMethod.Reliable);

        if (NetworkManager.Instance.AuthorizationType == AuthorizationType.HostAuthorization)
        {
            StartCoroutine(UpdateLobbyCoroutine(lobbySettings));
        }
    }

    public void JoinLobby(uint lobbyId)
    {
        StartCoroutine(JoinLobbyCoroutine(lobbyId));
    }

    private IEnumerator CreateUserCoroutine(UserSettings userSettings, bool invokeEvent = true)
    {
        string json = JsonConvert.SerializeObject(userSettings);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest($"{LobbyApiUrl}/user/create", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonBytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var userResponse = JsonConvert.DeserializeObject<UserResponse>(www.downloadHandler.text);
                UserData createdUserData = new UserData
                {
                    GlobalGuid = userResponse.GlobalGuid,
                    Settings = userResponse.UserSettings,
                };
                cachedUserData = createdUserData;
                webToken = userResponse.Token;

                if (invokeEvent)
                {
                    OnUserCreated?.Invoke(createdUserData);
                }
            }
            else
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: Failed to create user: {www.error}");
            }
        }
    }

    private IEnumerator UpdateUserCoroutine(UserSettings userSettings, bool invokeEvent = true)
    {
        string json = JsonConvert.SerializeObject(userSettings);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest($"{LobbyApiUrl}/user/update", "PUT"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonBytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {webToken}");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                cachedUserData.Settings = userSettings;

                if (invokeEvent)
                {
                    OnUserUpdated?.Invoke(cachedUserData);
                }
            }
            else
            {
                // User expired, create a new user
                if (www.responseCode == 401)
                {
                    yield return CreateUserCoroutine(userSettings, false);

                    if (cachedUserData != null)
                    {
                        yield return UpdateUserCoroutine(userSettings, invokeEvent);
                    }
                    else
                    {
                        Debug.LogError($"<color=red><b>CNS</b></color>: Failed to update user and re-create user.");
                    }
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Failed to update user: {www.error}");
                }
            }
        }
    }

    private IEnumerator CreateLobbyCoroutine(LobbySettings lobbySettings, bool invokeEvent = true)
    {
        string json = JsonConvert.SerializeObject(lobbySettings);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest($"{LobbyApiUrl}/lobby/create", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonBytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {webToken}");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var lobbyResponse = JsonConvert.DeserializeObject<LobbyResponse>(www.downloadHandler.text);
                LobbyData createdLobbyData = new LobbyData
                {
                    LobbyId = lobbyResponse.LobbyId,
                    Settings = lobbyResponse.LobbySettings
                };
                GameServerData createdGameServerData = new GameServerData
                {
                    GameServerId = lobbyResponse.GameServerId,
                    GameServerAddress = lobbyResponse.GameServerAddress
                };

                if (invokeEvent)
                {
                    cachedLobbyData = createdLobbyData;
                    cachedGameServerData = createdGameServerData;
#if CNS_TOKEN_VERIFIER
                    gameServerToken = lobbyResponse.GameServerToken;
                    OnLobbyCreated?.Invoke(createdLobbyData, createdGameServerData, gameServerToken);
#else
                    OnLobbyCreated?.Invoke(createdLobbyData, createdGameServerData);
#endif
                }
            }
            else
            {
                // User expired, create a new user
                if (www.responseCode == 401)
                {
                    yield return CreateUserCoroutine(cachedUserData.Settings, false);

                    if (cachedUserData != null)
                    {
                        yield return CreateLobbyCoroutine(lobbySettings, invokeEvent);
                    }
                    else
                    {
                        Debug.LogError($"<color=red><b>CNS</b></color>: Failed to create lobby and re-create user.");
                    }
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Failed to create lobby: {www.error}");
                }
            }
        }
    }

    private IEnumerator UpdateLobbyCoroutine(LobbySettings lobbySettings, bool invokeEvent = true)
    {
        string json = JsonConvert.SerializeObject(lobbySettings);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest($"{LobbyApiUrl}/lobby/update", "PUT"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonBytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {webToken}");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                cachedLobbyData.Settings = lobbySettings;

                if (invokeEvent)
                {
#if CNS_TOKEN_VERIFIER
                    OnLobbyUpdated?.Invoke(cachedLobbyData, cachedGameServerData, gameServerToken);
#else
                    OnLobbyUpdated?.Invoke(cachedLobbyData, cachedGameServerData);
#endif
                }
            }
            else
            {
                // User expired, create a new user
                if (www.responseCode == 401)
                {
                    yield return CreateUserCoroutine(cachedUserData.Settings, false);

                    if (cachedUserData != null)
                    {
                        yield return UpdateLobbyCoroutine(lobbySettings, invokeEvent);
                    }
                    else
                    {
                        Debug.LogError($"<color=red><b>CNS</b></color>: Failed to update lobby and re-create user.");
                    }
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Failed to create lobby: {www.error}");
                }
            }
        }
    }

    private IEnumerator JoinLobbyCoroutine(uint lobbyId, bool invokeEvent = true)
    {
        using (var www = new UnityWebRequest($"{LobbyApiUrl}/lobby/join/{lobbyId}", "GET"))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Authorization", $"Bearer {webToken}");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // TODO: Come back to this
                var lobbyResponse = JsonConvert.DeserializeObject<LobbyResponse>(www.downloadHandler.text);
                LobbyData joinedLobbyData = new LobbyData
                {
                    LobbyId = lobbyResponse.LobbyId,
                    Settings = lobbyResponse.LobbySettings
                };
                GameServerData joinedGameServerData = new GameServerData
                {
                    GameServerId = lobbyResponse.GameServerId,
                    GameServerAddress = lobbyResponse.GameServerAddress
                };

                if (invokeEvent)
                {
                    cachedLobbyData = joinedLobbyData;
                    cachedGameServerData = joinedGameServerData;
#if CNS_TOKEN_VERIFIER
                    gameServerToken = lobbyResponse.GameServerToken;
                    OnLobbyJoined?.Invoke(joinedLobbyData, joinedGameServerData, gameServerToken);
#else
                    OnLobbyJoined?.Invoke(joinedLobbyData, joinedGameServerData);
#endif
                }
            }
            else
            {
                // User expired, create a new user
                if (www.responseCode == 401)
                {
                    yield return CreateUserCoroutine(cachedUserData.Settings, false);

                    if (cachedUserData != null)
                    {
                        yield return JoinLobbyCoroutine(lobbyId, invokeEvent);
                    }
                    else
                    {
                        Debug.LogError($"<color=red><b>CNS</b></color>: Failed to join lobby and re-create user.");
                    }
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Failed to join lobby: {www.error}");
                }
            }
        }
    }
}
