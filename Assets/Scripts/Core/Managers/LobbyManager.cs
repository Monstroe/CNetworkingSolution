using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [SerializeField] private float fadeDuration = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of LobbyManager detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    public void GoToGameScene()
    {
        FadeScreen.Instance.Display(true, fadeDuration, () =>
        {
            if (ServerManager.Instance != null)
            {
                SceneManager.UnloadSceneAsync(NetResources.Instance.MenuSceneName);
                SceneManager.LoadSceneAsync(NetResources.Instance.GameSceneName, LoadSceneMode.Additive).completed += (asyncOperation) =>
                {
                    SceneManager.SetActiveScene(SceneManager.GetSceneByName(NetResources.Instance.GameSceneName));
                };
            }
            else
            {
                SceneManager.LoadSceneAsync(NetResources.Instance.GameSceneName);
            }
        });
    }

    public void GoToMenuScene()
    {
        FadeScreen.Instance.Display(true, fadeDuration, () =>
        {
            SceneManager.LoadSceneAsync(NetResources.Instance.MenuSceneName);
        });
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ClientManager.Instance.OnNewUserCreated += NewUserCreated;
        ClientManager.Instance.OnLobbyCreateRequested += LobbyCreateRequested;
        ClientManager.Instance.OnLobbyJoinRequested += LobbyJoinedRequested;
        ClientManager.Instance.OnLobbyConnectionAccepted += LobbyConnectionAccepted;
        ClientManager.Instance.OnLobbyConnectionRejected += LobbyConnectionRejected;
        ClientManager.Instance.OnLobbyConnectionLost += LobbyConnectionLost;
#if CNS_SERVER_MULTIPLE
        ClientManager.Instance.WebAPI.OnWebAPIConnectionError += OnWebAPIConnectionError;
#endif
    }

    void OnDestroy()
    {
        ClientManager.Instance.OnNewUserCreated -= NewUserCreated;
        ClientManager.Instance.OnLobbyCreateRequested -= LobbyCreateRequested;
        ClientManager.Instance.OnLobbyJoinRequested -= LobbyJoinedRequested;
        ClientManager.Instance.OnLobbyConnectionAccepted -= LobbyConnectionAccepted;
        ClientManager.Instance.OnLobbyConnectionRejected -= LobbyConnectionRejected;
        ClientManager.Instance.OnLobbyConnectionLost -= LobbyConnectionLost;
#if CNS_SERVER_MULTIPLE
        ClientManager.Instance.WebAPI.OnWebAPIConnectionError -= OnWebAPIConnectionError;
#endif
    }

    private void NewUserCreated(Guid userId)
    {
        Debug.Log($"New user created with ID: {userId}");
    }

    private void LobbyCreateRequested(TransportSettings serverSettings)
    {
        Debug.Log($"Creating lobby...");
        if (ClientManager.Instance.NetMode == NetMode.Local)
        {
            Instantiate(NetResources.Instance.ServerPrefab);
            ServerManager.Instance.RegisterTransport(TransportType.Local);
            ClientManager.Instance.RegisterTransport(TransportType.Local);
            return;
        }

#if CNS_SYNC_DEDICATED
        ClientManager.Instance.RegisterTransport(TransportType.LiteNetLib);
#elif CNS_SYNC_HOST && CNS_LOBBY_SINGLE
        Instantiate(NetResources.Instance.ServerPrefab);
        ServerManager.Instance.RegisterTransport(TransportType.LiteNetLib);
        ServerManager.Instance.RegisterTransport(TransportType.Local);
        ClientManager.Instance.RegisterTransport(TransportType.Local);
#elif CNS_SYNC_HOST && CNS_LOBBY_MULTIPLE
        ClientManager.Instance.RegisterTransport(TransportType.LiteNetLibRelay);
#endif
    }

    private void LobbyJoinedRequested(int lobbyId, TransportSettings serverSettings)
    {
        Debug.Log($"Joining lobby {lobbyId}...");
        ClientManager.Instance.RegisterTransport(TransportType.LiteNetLib);
    }

    private void LobbyConnectionAccepted(int lobbyId)
    {
        Debug.Log($"Successfully connected to lobby {lobbyId}.");
        GoToGameScene();
    }

    private void LobbyConnectionRejected(int lobbyId, LobbyRejectionType errorType)
    {
        Debug.Log($"Connection to lobby {lobbyId} rejected: {errorType}");
    }

    private void LobbyConnectionLost(TransportCode code)
    {
        Debug.LogWarning($"Lost connection to lobby: {code}");

        if (ServerManager.Instance != null)
        {
            Destroy(ServerManager.Instance.gameObject);
        }

        if (SceneManager.GetActiveScene().name != NetResources.Instance.MenuSceneName)
        {
            GoToMenuScene();
        }
    }

#if CNS_SERVER_MULTIPLE
    private void OnWebAPIConnectionError(string errorMessage)
    {
        Debug.LogError($"WebAPI connection error: {errorMessage}");
    }
#endif
}
