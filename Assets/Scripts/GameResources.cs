using UnityEngine;

public class GameResources : MonoBehaviour
{
    public static GameResources Instance { get; private set; }

    public LayerMask GroundMask => groundMask;
    public LayerMask InteractionMask => interactionMask;
    public string GameSceneName => gameSceneName;
    public string MenuSceneName => menuSceneName;
    public string ServerSceneName => serverSceneName;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask interactionMask;
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private string serverSceneName = "Server";

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
