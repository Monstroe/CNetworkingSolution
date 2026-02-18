#if CNS_SERVER_MULTIPLE
using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class ClientWebAPI
{
    public delegate void WebAPIConnectionErrorEventHandler(string errorMessage);
    public event WebAPIConnectionErrorEventHandler OnWebAPIConnectionError;

    class UserResponse
    {
        public Guid GlobalGuid { get; set; }
        public UserSettings UserSettings { get; set; }
        public string Token { get; set; }
    }

    class LobbyResponse
    {
        public int LobbyId { get; set; }
        public LobbySettings LobbySettings { get; set; }
#nullable enable
        public TransportSettings? ServerSettings { get; set; }
        public string? ServerToken { get; set; }
#nullable disable
    }

    public string ConnectionToken { get; private set; }
    public string LobbyApiUrl { get; private set; }
    private string webToken;

    public ClientWebAPI(string lobbyApiUrl)
    {
        LobbyApiUrl = lobbyApiUrl;
    }

    public IEnumerator CreateUserCoroutine(UserSettings userSettings, Action<Guid, UserSettings> onUserCreate)
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
                webToken = userResponse.Token;
                onUserCreate.Invoke(userResponse.GlobalGuid, userResponse.UserSettings);
            }
            else
            {
                Debug.LogError($"<color=red><b>CNS</b></color>: Failed to create user: {www.error}");
                OnWebAPIConnectionError?.Invoke(www.error);
            }
        }
    }

    public IEnumerator UpdateUserCoroutine(UserSettings userSettings, Action<Guid, UserSettings> onUserRecreate, Action<UserSettings> onUserUpdate)
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
                onUserUpdate.Invoke(userSettings);
            }
            else
            {
                // User expired, create a new user
                if (www.responseCode == 401)
                {
                    yield return CreateUserCoroutine(userSettings, onUserRecreate);
                    yield return UpdateUserCoroutine(userSettings, onUserRecreate, onUserUpdate);
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Failed to update user: {www.error}");
                    OnWebAPIConnectionError?.Invoke(www.error);
                }
            }
        }
    }

#nullable enable
    public IEnumerator CreateLobbyCoroutine(LobbySettings lobbySettings, UserSettings userSettings, Action<Guid, UserSettings> onUserRecreate, Action<int, LobbySettings, TransportSettings?> onLobbyCreate)
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
                ConnectionToken = lobbyResponse!.ServerToken;
                onLobbyCreate.Invoke(lobbyResponse.LobbyId, lobbyResponse.LobbySettings, lobbyResponse.ServerSettings);
            }
            else
            {
                // User expired, create a new user
                if (www.responseCode == 401)
                {
                    yield return CreateUserCoroutine(userSettings, onUserRecreate);
                    yield return CreateLobbyCoroutine(lobbySettings, userSettings, onUserRecreate, onLobbyCreate);
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Failed to create lobby: {www.error}");
                    OnWebAPIConnectionError?.Invoke(www.error);
                }
            }
        }
    }
#nullable disable

    public IEnumerator UpdateLobbyCoroutine(LobbySettings lobbySettings, UserSettings userSettings, Action<Guid, UserSettings> onUserRecreate, Action<LobbySettings> onLobbyUpdate)
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
                onLobbyUpdate.Invoke(lobbySettings);
            }
            else
            {
                // User expired, create a new user
                if (www.responseCode == 401)
                {
                    yield return CreateUserCoroutine(userSettings, onUserRecreate);
                    yield return UpdateLobbyCoroutine(lobbySettings, userSettings, onUserRecreate, onLobbyUpdate);
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Failed to create lobby: {www.error}");
                    OnWebAPIConnectionError?.Invoke(www.error);
                }
            }
        }
    }

#nullable enable
    public IEnumerator JoinLobbyCoroutine(int lobbyId, UserSettings userSettings, Action<Guid, UserSettings> onUserRecreate, Action<int, LobbySettings, TransportSettings?> onLobbyJoin)
    {
        using (var www = new UnityWebRequest($"{LobbyApiUrl}/lobby/join/{lobbyId}", "GET"))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Authorization", $"Bearer {webToken}");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var lobbyResponse = JsonConvert.DeserializeObject<LobbyResponse>(www.downloadHandler.text);
                ConnectionToken = lobbyResponse!.ServerToken;
                onLobbyJoin.Invoke(lobbyResponse.LobbyId, lobbyResponse.LobbySettings, lobbyResponse.ServerSettings);
            }
            else
            {
                // User expired, create a new user
                if (www.responseCode == 401)
                {
                    yield return CreateUserCoroutine(userSettings, onUserRecreate);
                    yield return JoinLobbyCoroutine(lobbyId, userSettings, onUserRecreate, onLobbyJoin);
                }
                else
                {
                    Debug.LogError($"<color=red><b>CNS</b></color>: Failed to join lobby: {www.error}");
                    OnWebAPIConnectionError?.Invoke(www.error);
                }
            }
        }
    }
#nullable disable
}
#endif
