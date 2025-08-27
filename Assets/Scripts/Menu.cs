using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 3f;

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
        SceneManager.LoadScene("Server", LoadSceneMode.Additive);
        ClientManager.Instance.OnUserCreated += (user) =>
        {
            ClientManager.Instance.JoinLobby(0);
        };
        ClientManager.Instance.OnLobbyConnectionEstablished += (lobbyId) =>
        {
            Debug.Log($"Successfully connected to lobby {lobbyId}.");
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
        ClientManager.Instance.OnUserCreated += (user) =>
        {
            ClientManager.Instance.JoinLobby(0);
        };
        ClientManager.Instance.OnLobbyConnectionEstablished += (lobbyId) =>
        {
            Debug.Log($"Successfully connected to lobby {lobbyId}.");
            SceneManager.LoadScene(GameResources.Instance.GameSceneName);
        };
        ClientManager.Instance.CreateNewUser();
    }
}
