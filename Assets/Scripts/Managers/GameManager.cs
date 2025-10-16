using System.Collections;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public delegate void GameInitializedEventHandler();
    public event GameInitializedEventHandler OnGameInitialized;

    public static GameManager Instance { get; private set; }

    public bool Initialized { get; private set; } = false;

    [Header("Game Initialization")]
    [SerializeField] private float pregameLoadDuration = 3f;
    [SerializeField] private float gameLoadDuration = 1f;
    [SerializeField] private float fadeDuration = 1f;

    private bool initLoopCanStart = false;
    private bool initLoopCanEnd = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of GameManager detected. Destroying duplicate instance.");
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SceneManager.sceneLoaded += SceneLoaded;

        // NOTE: These two calls aren't necessary, I'm just showcasing features here
        ClientManager.Instance.OnCurrentUserUpdated += CurrentUserUpdated;
        ClientManager.Instance.OnCurrentLobbyUpdated += CurrentLobbyUpdated;
        ClientManager.Instance.OnLobbyConnectionLost += LobbyConnectionLost;
    }

    private void CurrentUserUpdated(UserSettings userSettings)
    {
        Debug.Log($"Updating settings for local user: UserName - {userSettings.UserName}");
    }

    private void CurrentLobbyUpdated(LobbySettings lobbySettings)
    {
        Debug.Log($"Successfully updated lobby settings: MaxUsers - {lobbySettings.MaxUsers}, LobbyVisibility - {lobbySettings.LobbyVisibility}, LobbyName - {lobbySettings.LobbyName}");
    }

    private void LobbyConnectionLost()
    {
        Debug.LogWarning("Lost connection to lobby. Returning to main menu...");

        ClientManager.Instance.RemoveTransport();
        if (ServerManager.Instance != null)
        {
            Destroy(ServerManager.Instance.gameObject);
        }

        FadeScreen.Instance.Display(true, fadeDuration, () =>
        {
            SceneManager.LoadSceneAsync(GameResources.Instance.MenuSceneName);
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (initLoopCanStart)
        {
            initLoopCanStart = false;
            FadeScreen.Instance.Display(false, fadeDuration, () =>
            {
                Debug.Log("Scene loaded, initializing game...");
                StartCoroutine(InitGame());
            });
        }

        if (initLoopCanEnd)
        {
            initLoopCanEnd = false;
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.GameUserJoined(ClientManager.Instance.CurrentUser), TransportMethod.Reliable);
            StartCoroutine(StartGame());
        }
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals(GameResources.Instance.GameSceneName))
        {
            LoadPreGame();
            initLoopCanStart = true;
        }
    }

    private IEnumerator InitGame()
    {
        yield return new WaitForSeconds(pregameLoadDuration);

        // NOTE: These two calls aren't necessary, I'm just showcasing features here
        ClientManager.Instance.UpdateCurrentUser(new UserSettings() { UserName = $"Player-{ClientManager.Instance.CurrentUser.GlobalGuid.ToString().Substring(0, 8)}" });
        ClientManager.Instance.UpdateCurrentLobby(new LobbySettings() { LobbyName = "MyLobby", LobbyVisibility = LobbyVisibility.PUBLIC, MaxUsers = 8 });

        FadeScreen.Instance.Display(true, fadeDuration, () =>
        {
            LoadGame();
            initLoopCanEnd = true;
        });
    }

    private IEnumerator StartGame()
    {
        yield return new WaitForSeconds(gameLoadDuration);
        Initialized = true;
        OnGameInitialized?.Invoke();
        FadeScreen.Instance.Display(false, fadeDuration, () =>
        {
            Debug.Log("Game initialized successfully.");
        });
    }

    private void LoadPreGame()
    {
        GameUI.Instance.ShowGame(false);
        GameContent.Instance.ShowGame(false);
        GameUI.Instance.ShowPregame(true);
        GameContent.Instance.ShowPregame(true);
    }

    private void LoadGame()
    {
        GameUI.Instance.ShowPregame(false);
        GameContent.Instance.ShowPregame(false);
        GameUI.Instance.ShowGame(true);
        GameContent.Instance.ShowGame(true);
    }
}
