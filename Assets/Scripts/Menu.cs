using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 3f;
    [Space]
    [SerializeField] private GameObject localServerPrefab;

#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_HOST
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject multiLobbyMenu;
    [SerializeField] private TMP_InputField lobbyIdInputField;
#endif

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartSinglePlayer()
    {
        GameResources.Instance.GameMode = GameMode.SINGLEPLAYER;
        ClientManager.Instance.SetTransport(TransportType.Local);
        Instantiate(localServerPrefab);
        ServerManager.Instance.ClearTransports();
        ServerManager.Instance.AddTransport(TransportType.Local);

        ClientManager.Instance.OnNewUserCreated += (user) =>
        {
            ClientManager.Instance.JoinExistingLobby(GameResources.Instance.DefaultLobbyId);
        };
        ClientManager.Instance.OnLobbyConnectionEstablished += (lobbyId) =>
        {
            Debug.Log($"Successfully connected to lobby {lobbyId}.");

            FadeScreen.Instance.Display(true, fadeDuration, () =>
            {
                SceneManager.LoadScene(GameResources.Instance.GameSceneName);
            });
        };

        ClientManager.Instance.CreateNewUser();
    }

    public void StartMultiPlayer()
    {

#if CNS_SYNC_SERVER_MULTIPLE || CNS_SYNC_HOST
        mainMenu.SetActive(false);
        multiLobbyMenu.SetActive(true);
#elif CNS_SYNC_SERVER_SINGLE
        ClientManager.Instance.OnNewUserCreated += (user) =>
        {
            ClientManager.Instance.JoinExistingLobby(GameResources.Instance.DefaultLobbyId);
        };
        ClientManager.Instance.OnLobbyConnectionEstablished += (lobbyId) =>
        {
            Debug.Log($"Successfully connected to lobby {lobbyId}.");

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
        };
        ClientManager.Instance.CreateNewUser();
#endif
    }

    public void BackToMainMenu()
    {
        multiLobbyMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void CreateLobby()
    {
        ClientManager.Instance.OnNewUserCreated += (user) =>
        {
            ClientManager.Instance.CreateNewLobby();
        };
        ClientManager.Instance.OnLobbyConnectionEstablished += (lobbyId) =>
        {
            Debug.Log($"Successfully created lobby {lobbyId}.");

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
        };
        ClientManager.Instance.CreateNewUser();
    }

    public void JoinLobby()
    {
        if (!int.TryParse(lobbyIdInputField.text, out int parsedId))
        {
            return;
        }

        ClientManager.Instance.OnNewUserCreated += (user) =>
        {
            ClientManager.Instance.JoinExistingLobby(parsedId);
        };
        ClientManager.Instance.OnLobbyConnectionEstablished += (lobbyId) =>
        {
            Debug.Log($"Successfully joined lobby {lobbyId}.");

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
        };
        ClientManager.Instance.CreateNewUser();
    }
}
