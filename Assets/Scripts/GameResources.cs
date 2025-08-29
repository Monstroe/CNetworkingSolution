using UnityEngine;

public class GameResources : MonoBehaviour
{
    public static GameResources Instance { get; private set; }

    public LayerMask GroundMask => groundMask;
    public LayerMask InteractionMask => interactionMask;

    public string GameSceneName => gameSceneName;
    public string MenuSceneName => menuSceneName;
    public string ServerSceneName => serverSceneName;

    public int DefaultLobbyId => defaultLobbyId;
    public LobbySettings DefaultLobbySettings => defaultLobbySettings;
    public UserSettings DefaultUserSettings => defaultUserSettings;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask interactionMask;
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private string serverSceneName = "Server";
    [Header("Default Settings")]
    [SerializeField] private int defaultLobbyId = 0;
    [SerializeField] private LobbySettings defaultLobbySettings = new LobbySettings();
    [Space]
    [SerializeField] private UserSettings defaultUserSettings = new UserSettings();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of GameResources detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }
}
