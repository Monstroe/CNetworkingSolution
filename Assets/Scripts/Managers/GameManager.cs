using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public delegate void GameInitializedEventHandler();
    public event GameInitializedEventHandler OnGameInitialized;

    public static GameManager Instance { get; private set; }

    public bool Initialized { get; private set; } = false;

    [Header("Game Initialization")]
    [SerializeField] private float pregameDuration = 3f;
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
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.GameUserJoined(), TransportMethod.Reliable);
            FadeScreen.Instance.Display(false, fadeDuration, () =>
            {
                Debug.Log("Game initialized successfully.");
                Initialized = true;
                OnGameInitialized?.Invoke();
            });
        }
    }

    public void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals(GameResources.Instance.GameSceneName))
        {
            StartPreGame();
            initLoopCanStart = true;
        }
    }

    private IEnumerator InitGame()
    {
        yield return new WaitForSeconds(pregameDuration);
        FadeScreen.Instance.Display(true, fadeDuration, () =>
        {
            StartGame();
            initLoopCanEnd = true;
        });
    }

    private void StartPreGame()
    {
        GameUI.Instance.ShowGame(false);
        GameContent.Instance.ShowGame(false);
        GameUI.Instance.ShowPregame(true);
        GameContent.Instance.ShowPregame(true);
    }

    private void StartGame()
    {
        GameUI.Instance.ShowPregame(false);
        GameContent.Instance.ShowPregame(false);
        GameUI.Instance.ShowGame(true);
        GameContent.Instance.ShowGame(true);
    }
}
