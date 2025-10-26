using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 3f;

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject multiplayerMenu;
    [SerializeField] private GameObject singleHostMenu;
    [SerializeField] private TMP_InputField lobbyIdInputField;
    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

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
        if (NetResources.Instance.GameMode == GameMode.Singleplayer)
        {
            Instantiate(NetResources.Instance.ServerPrefab);
            ServerManager.Instance.RegisterTransport(TransportType.Local);
            ClientManager.Instance.RegisterTransport(TransportType.Local);
            return;
        }

#if CNS_SYNC_DEDICATED
        ClientManager.Instance.RegisterTransport(TransportType.CNet);
#elif CNS_SYNC_HOST && CNS_LOBBY_SINGLE
        Instantiate(NetResources.Instance.ServerPrefab);
        ServerManager.Instance.RegisterTransport(TransportType.CNet);
        ServerManager.Instance.RegisterTransport(TransportType.Local);
        ClientManager.Instance.RegisterTransport(TransportType.Local);
#elif CNS_SYNC_HOST && CNS_LOBBY_MULTIPLE
        ClientManager.Instance.RegisterTransport(TransportType.LiteNetLibRelay);
#endif
    }

    private void LobbyJoinedRequested(int lobbyId, ServerSettings serverSettings)
    {
        Debug.Log($"Joining lobby {lobbyId}...");
        ClientManager.Instance.RegisterTransport(TransportType.CNet);
    }

    private void LobbyConnectionAccepted(int lobbyId)
    {
        Debug.Log($"Connected to lobby {lobbyId}.");

        FadeScreen.Instance.Display(true, fadeDuration, () =>
        {
            SceneManager.UnloadSceneAsync(NetResources.Instance.MenuSceneName);
            SceneManager.LoadSceneAsync(NetResources.Instance.GameSceneName, LoadSceneMode.Additive).completed += (asyncOperation) =>
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(NetResources.Instance.GameSceneName));
            };
        });
    }

    private void LobbyConnectionRejected(int lobbyId, LobbyRejectionType errorType)
    {
        Debug.Log($"Failed to connect to lobby {lobbyId}. Reason: {errorType}");
    }

    public void StartSinglePlayer()
    {
        NetResources.Instance.GameMode = GameMode.Singleplayer;
        ClientManager.Instance.CreateNewUser();
        ClientManager.Instance.CreateNewLobby();
    }

    public void StartMultiPlayer()
    {
        ClientManager.Instance.CreateNewUser();
#if CNS_SERVER_MULTIPLE || CNS_LOBBY_MULTIPLE
        ToMultiplayerMenu();
#elif CNS_SERVER_SINGLE && CNS_LOBBY_SINGLE && CNS_SYNC_HOST
        ToSingleHostMenu();
#elif CNS_SERVER_SINGLE && CNS_LOBBY_SINGLE
        ClientManager.Instance.JoinExistingLobby(NetResources.Instance.DefaultLobbyId);
#endif
    }

    public void ToMultiplayerMenu()
    {
        mainMenu.SetActive(false);
        multiplayerMenu.SetActive(true);
    }

    public void ToSingleHostMenu()
    {
        mainMenu.SetActive(false);
        singleHostMenu.SetActive(true);
    }

    public void BackToMainMenu()
    {
        multiplayerMenu.SetActive(false);
        singleHostMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void CreateLobby()
    {
        ClientManager.Instance.CreateNewLobby();
    }

    public void JoinLobby()
    {
#if CNS_SERVER_SINGLE && CNS_LOBBY_SINGLE && CNS_SYNC_HOST
        ClientManager.Instance.JoinExistingLobby(NetResources.Instance.DefaultLobbyId);
#else
        if (!int.TryParse(lobbyIdInputField.text, out int parsedId))
        {
            return;
        }

        ClientManager.Instance.JoinExistingLobby(parsedId);
#endif
    }
}
