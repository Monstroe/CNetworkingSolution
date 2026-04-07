using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CNetworkingSolution;

public class ClientInitializer : MonoBehaviour
{
    [Header("Game Initialization")]
    [SerializeField] private float pregameLoadDuration = 1f;
    [SerializeField] private float gameLoadDuration = 1f;
    [SerializeField] private float fadeDuration = 1f;
    [Header("Scene Management")]
    [SerializeField] private string mainSceneName = "CNSSampleScene";

    private bool initLoopCanStart = false;
    private bool initLoopCanEnd = false;
    private bool initialized = false;

    void Start()
    {
        ClientManager.Instance.OnConnectionAccepted += ConnectionAccepted;
        ClientManager.Instance.OnConnectionLost += ConnectionLost;
        LoadPreGame();
    }

    void OnDestroy()
    {
        if (ClientManager.Instance != null)
        {
            ClientManager.Instance.OnConnectionAccepted -= ConnectionAccepted;
            ClientManager.Instance.OnConnectionLost -= ConnectionLost;
        }
    }

    private void ConnectionAccepted(ConnectionAcceptedArgs args)
    {
        initialized = false;
        ClientManager.Instance.CurrentLobby.GetService<GameClientService>().OnGameInitialized += GameInitialized;
        initLoopCanStart = true;
    }

    private void ConnectionLost(ConnectionLostArgs args)
    {
        if (ServerManager.Instance != null)
        {
            Destroy(ServerManager.Instance.gameObject);
        }

        if (!initialized)
        {
            ClientManager.Instance.CurrentLobby.GetService<GameClientService>().OnGameInitialized -= GameInitialized;
        }

        FadeScreen.Instance.Display(true, fadeDuration, () =>
        {
            SceneManager.LoadSceneAsync(mainSceneName);
        });
    }

    private void GameInitialized()
    {
        initialized = true;
        ClientManager.Instance.CurrentLobby.GetService<GameClientService>().OnGameInitialized -= GameInitialized;
        StartCoroutine(StartGame());
    }

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
            ClientManager.Instance.CurrentLobby.GetService<GameClientService>().JoinGame();
        }
    }

    private IEnumerator InitGame()
    {
        yield return new WaitForSeconds(pregameLoadDuration);
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
