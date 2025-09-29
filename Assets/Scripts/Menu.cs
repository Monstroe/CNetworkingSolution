using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 3f;
    [Space]
    [SerializeField] private GameObject localServerPrefab;

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject multiplayerMenu;
    [SerializeField] private TMP_InputField lobbyIdInputField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ClientManager.Instance.OnNewUserCreated += NewUserCreated;
        ClientManager.Instance.OnLobbyCreateRequested += LobbyCreateRequested;
        ClientManager.Instance.OnLobbyJoinRequested += LobbyJoinedRequested;
        ClientManager.Instance.OnLobbyConnectionAccepted += LobbyConnectionAccepted;
        ClientManager.Instance.OnLobbyConnectionRejected += LobbyConnectionRejected;
    }

    void OnDestroy()
    {
        ClientManager.Instance.OnNewUserCreated -= NewUserCreated;
        ClientManager.Instance.OnLobbyCreateRequested -= LobbyCreateRequested;
        ClientManager.Instance.OnLobbyJoinRequested -= LobbyJoinedRequested;
        ClientManager.Instance.OnLobbyConnectionAccepted -= LobbyConnectionAccepted;
        ClientManager.Instance.OnLobbyConnectionRejected -= LobbyConnectionRejected;
    }

    private void NewUserCreated(Guid userId)
    {
        Debug.Log($"New user created with ID: {userId}");
    }

    private void LobbyCreateRequested(ServerSettings serverSettings)
    {
        Debug.Log($"Creating lobby...");
    }

    private void LobbyJoinedRequested(int lobbyId, ServerSettings serverSettings)
    {
        Debug.Log($"Joining lobby {lobbyId}...");
    }

    private void LobbyConnectionAccepted(int lobbyId)
    {
        Debug.Log($"Connected to lobby {lobbyId}.");

        FadeScreen.Instance.Display(true, fadeDuration, () =>
        {
            if (SceneManager.GetSceneByName(GameResources.Instance.ServerSceneName).isLoaded)
            {
                SceneManager.UnloadSceneAsync(GameResources.Instance.MenuSceneName);
                SceneManager.LoadSceneAsync(GameResources.Instance.GameSceneName, LoadSceneMode.Additive);
            }
            else
            {
                SceneManager.LoadScene(GameResources.Instance.GameSceneName);
            }
        });
    }

    private void LobbyConnectionRejected(int lobbyId, LobbyRejectionType errorType)
    {
        Debug.Log($"Failed to connect to lobby {lobbyId}. Reason: {errorType}");
    }

    public void StartSinglePlayer()
    {
        GameResources.Instance.GameMode = GameMode.SINGLEPLAYER;
        ClientManager.Instance.SetTransport(TransportType.CNet);
        Instantiate(localServerPrefab);
        ServerManager.Instance.ClearTransports();
        ServerManager.Instance.AddTransport(TransportType.CNet);
        ClientManager.Instance.CreateNewUser();
        ClientManager.Instance.CreateNewLobby();
    }

    public void StartMultiPlayer()
    {
        ClientManager.Instance.CreateNewUser();
#if CNS_SYNC_SERVER_MULTIPLE || CNS_LOBBY_MULTIPLE
        ToMultiplayerMenu();
#elif CNS_SYNC_SERVER_SINGLE && CNS_LOBBY_SINGLE
        ClientManager.Instance.JoinExistingLobby(GameResources.Instance.DefaultLobbyId);
#endif
    }

    public void ToMultiplayerMenu()
    {
        mainMenu.SetActive(false);
        multiplayerMenu.SetActive(true);
    }

    public void BackToMainMenu()
    {
        multiplayerMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void CreateLobby()
    {
        ClientManager.Instance.CreateNewLobby();
    }

    public void JoinLobby()
    {
        if (!int.TryParse(lobbyIdInputField.text, out int parsedId))
        {
            return;
        }

        ClientManager.Instance.JoinExistingLobby(parsedId);
    }
}
