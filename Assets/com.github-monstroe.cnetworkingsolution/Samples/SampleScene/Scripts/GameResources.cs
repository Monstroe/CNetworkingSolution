using UnityEngine;

public class GameResources : MonoBehaviour
{
    public static GameResources Instance { get; private set; }

    public string MenuSceneName => menuSceneName;
    public string GameSceneName => gameSceneName;

    public LayerMask GroundMask => groundMask;
    public LayerMask InteractionMask => interactionMask;

    [Header("Scene Names")]
    [SerializeField] private string menuSceneName = "MenuScene";
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Layer Masks")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask interactionMask;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of GameResources detected. Destroying duplicate instance.");
            Destroy(gameObject);
            return;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
