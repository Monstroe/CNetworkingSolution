using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    public static GameInitializer Instance { get; private set; }

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
            Debug.LogWarning("Multiple instances of GameInitializer detected. Destroying duplicate instance.");
            Destroy(gameObject);
            return;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SceneManager.sceneLoaded += SceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals(NetResources.Instance.GameSceneName))
        {
            ClientManager.Instance.CurrentLobby.GetService<GameClientService>().OnGameInitialized += GameInitialized;
            LoadPreGame();
            initLoopCanStart = true;
        }
        else if (scene.name.Equals(NetResources.Instance.MenuSceneName) && !Initialized)
        {
            ClientManager.Instance.CurrentLobby.GetService<GameClientService>().OnGameInitialized -= GameInitialized;
        }
    }

    private void GameInitialized()
    {
        Initialized = true;
        ClientManager.Instance.CurrentLobby.GetService<GameClientService>().OnGameInitialized -= GameInitialized;
        StartCoroutine(StartGame());
    }

    // Update is called once per frame
    void Update()
    {
        if (initLoopCanStart)
        {
            initLoopCanStart = false;
            FadeScreen.Instance.Display(false, fadeDuration, () =>
            {
                StartCoroutine(InitGame());
            });
        }

        if (initLoopCanEnd)
        {
            initLoopCanEnd = false;
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.GameUserJoined(ClientManager.Instance.CurrentLobby.CurrentUser), TransportMethod.Reliable);
        }
    }

    private IEnumerator InitGame()
    {
        yield return new WaitForSeconds(pregameLoadDuration);

        // NOTE: These two calls aren't necessary, I'm just showcasing features here
        ClientManager.Instance.UpdateCurrentUser(new UserSettings() { UserName = $"Player-{ClientManager.Instance.CurrentLobby.CurrentUser.GlobalGuid.ToString().Substring(0, 8)}" });
        var newLobbySettings = ClientManager.Instance.CurrentLobby.LobbyData.Settings.Clone();
        newLobbySettings.MaxUsers = 8;
        ClientManager.Instance.UpdateCurrentLobby(newLobbySettings);

        FadeScreen.Instance.Display(true, fadeDuration, () =>
        {
            LoadGame();
            initLoopCanEnd = true;
        });
    }

    private IEnumerator StartGame()
    {
        yield return new WaitForSeconds(gameLoadDuration);
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
