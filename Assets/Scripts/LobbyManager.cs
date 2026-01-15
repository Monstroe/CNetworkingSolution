using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

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
        FadeMenuUI.Instance.Display(true, () =>
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
        FadeMenuUI.Instance.Display(true, () =>
        {
            if (ServerManager.Instance != null)
            {
                SceneManager.UnloadSceneAsync(NetResources.Instance.GameSceneName);
                SceneManager.LoadSceneAsync(NetResources.Instance.MenuSceneName, LoadSceneMode.Additive).completed += (asyncOperation) =>
                {
                    SceneManager.SetActiveScene(SceneManager.GetSceneByName(NetResources.Instance.MenuSceneName));
                };
            }
            else
            {
                SceneManager.LoadSceneAsync(NetResources.Instance.MenuSceneName);
            }
        });
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        ClientManager.Instance.OnLobbyCreateRequested -= LobbyCreateRequested;
        ClientManager.Instance.OnLobbyJoinRequested -= LobbyJoinedRequested;
        ClientManager.Instance.OnLobbyConnectionAccepted -= LobbyConnectionAccepted;
        ClientManager.Instance.OnLobbyConnectionRejected -= LobbyConnectionRejected;
        ClientManager.Instance.OnLobbyConnectionLost -= LobbyConnectionLost;
#if CNS_SERVER_MULTIPLE
        ClientManager.Instance.WebAPI.OnWebAPIConnectionError -= OnWebAPIConnectionError;
#endif
    }

    private void LobbyCreateRequested(TransportSettings serverSettings)
    {
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
        ClientManager.Instance.RegisterTransport(TransportType.LiteNetLib);
    }

    private void LobbyConnectionAccepted(int lobbyId)
    {
        PopupUI.Instance.ShowPopup("Connected to Lobby", "Successfully connected to the lobby.", PopupType.None, PopupSeverity.Info, null, 1.0f);
    }

    private void LobbyConnectionRejected(int lobbyId, LobbyRejectionType errorType)
    {
        if (SceneManager.GetActiveScene().name != NetResources.Instance.GameSceneName)
        {
            string errorDesc = null;
            switch (errorType)
            {
                case LobbyRejectionType.LobbyFull:
                    errorDesc = "The lobby is full.";
                    break;
                case LobbyRejectionType.LobbyNotFound:
                    errorDesc = "Could not find lobby.";
                    break;
                case LobbyRejectionType.LobbyClosed:
                    errorDesc = "The lobby is not accepting connections.";
                    break;
                case LobbyRejectionType.KickedByHost:
                    errorDesc = "You have been kicked from the lobby by the host.";
                    break;
            }
            PopupUI.Instance.ShowPopup("Connection Rejected", errorDesc, PopupType.OK, PopupSeverity.Error);
        }
    }

    private void LobbyConnectionLost(TransportCode code)
    {
        if (ServerManager.Instance != null)
        {
            Destroy(ServerManager.Instance.gameObject);
        }

        if (SceneManager.GetActiveScene().name != NetResources.Instance.MenuSceneName)
        {
            PopupUI.Instance.ShowPopup("Connection Lost", "Lost connection to lobby. Returning to main menu.", PopupType.None, PopupSeverity.Error, (button) =>
            {
                GoToMenuScene();
            }, 1.5f);
        }
    }

#if CNS_SERVER_MULTIPLE
    private void OnWebAPIConnectionError(string errorMessage)
    {
        PopupUI.Instance.HidePopup();
        PopupUI.Instance.ShowPopup("Connection Error", errorMessage, PopupType.OK, PopupSeverity.Error);
    }
#endif
}
