using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ClientManager.Instance.OnCurrentUserUpdated += (userSettings) =>
        {
            Debug.Log($"Current user updated: UserName: {userSettings.UserName}");
        };

        ClientManager.Instance.OnCurrentLobbyUpdated += (lobbySettings) =>
        {
            Debug.Log($"Current lobby updated: MaxUsers: {lobbySettings.MaxUsers}, LobbyVisibility: {lobbySettings.LobbyVisibility}, LobbyName: {lobbySettings.LobbyName}");
        };
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartSinglePlayer()
    {
        SceneManager.LoadScene(GameResources.Instance.ServerSceneName, LoadSceneMode.Additive);
        ClientManager.Instance.OnNewUserCreated += (user) =>
        {
            ClientManager.Instance.JoinLobby(ClientManager.Instance.DefaultLobbyId);
        };
        ClientManager.Instance.OnLobbyConnectionEstablished += (lobbyId) =>
        {
            Debug.Log($"Successfully connected to lobby {lobbyId}.");
            ClientManager.Instance.UpdateCurrentUser(new UserSettings() { UserName = $"Player-{ClientManager.Instance.CurrentUser.GlobalGuid.ToString().Substring(0, 8)}" });
            ClientManager.Instance.UpdateCurrentLobby(new LobbySettings() { LobbyName = "SinglePlayer", LobbyVisibility = LobbyVisibility.PRIVATE, MaxUsers = 1 });

            FadeScreen.Instance.Display(true, fadeDuration, () =>
            {
                SceneManager.UnloadSceneAsync(GameResources.Instance.MenuSceneName);
                SceneManager.LoadSceneAsync(GameResources.Instance.GameSceneName, LoadSceneMode.Additive);
            });
        };
        ClientManager.Instance.CreateNewUser();
    }

    public void StartMultiPlayer()
    {
        ClientManager.Instance.OnNewUserCreated += (user) =>
        {
            ClientManager.Instance.JoinLobby(ClientManager.Instance.DefaultLobbyId);
        };
        ClientManager.Instance.OnLobbyConnectionEstablished += (lobbyId) =>
        {
            Debug.Log($"Successfully connected to lobby {lobbyId}.");
            ClientManager.Instance.UpdateCurrentUser(new UserSettings() { UserName = $"Player-{ClientManager.Instance.CurrentUser.GlobalGuid.ToString().Substring(0, 8)}" });
            ClientManager.Instance.UpdateCurrentLobby(new LobbySettings() { LobbyName = "MultiPlayer", LobbyVisibility = LobbyVisibility.PUBLIC, MaxUsers = 8 });

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
