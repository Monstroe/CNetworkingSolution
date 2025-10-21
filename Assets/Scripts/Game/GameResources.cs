using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class GameResources : MonoBehaviour
{
    public static GameResources Instance { get; private set; }

    public GameObject ServerPrefab => serverPrefab;

    public GameMode GameMode { get => gameMode; set => gameMode = value; }

    public LayerMask GroundMask => groundMask;
    public LayerMask InteractionMask => interactionMask;

    public string GameSceneName => gameSceneName;
    public string MenuSceneName => menuSceneName;
    public string ServerSceneName => serverSceneName;

    public int DefaultLobbyId => defaultLobbyId;
    public LobbySettings DefaultLobbySettings => defaultLobbySettings;
    public UserSettings DefaultUserSettings => defaultUserSettings;

    [SerializeField] private GameObject serverPrefab;

    [Header("Game Settings")]
    [SerializeField] private GameMode gameMode = GameMode.Multiplayer;

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

    [Header("FX Settings")]
    [SerializeField] private string sfxDirectory = "SFX/";
    [SerializeField] private string vfxDirectory = "VFX/";

    private readonly Dictionary<string, AudioClip> cachedSFX = new Dictionary<string, AudioClip>();
    private readonly Dictionary<int, AudioClip> cachedSFXById = new Dictionary<int, AudioClip>();
    private readonly Dictionary<string, int> sfxInstances = new Dictionary<string, int>();
    private readonly Dictionary<string, VisualEffectAsset> cachedVFX = new Dictionary<string, VisualEffectAsset>();
    private readonly Dictionary<int, VisualEffectAsset> cachedVFXById = new Dictionary<int, VisualEffectAsset>();
    private readonly Dictionary<string, int> vfxInstances = new Dictionary<string, int>();

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
            return;
        }

        InitSFXClips();
        InitVFXAssets();
    }

    private void InitSFXClips()
    {
        string[] folders = new string[] { "" };

        int id = 0;
        foreach (string folder in folders)
        {
            AudioClip[] clips = Resources.LoadAll<AudioClip>(sfxDirectory + folder);
            foreach (AudioClip clip in clips)
            {
                cachedSFX.Add(clip.name, clip);
                cachedSFXById.Add(id, clip);
                sfxInstances.Add(clip.name, id);
                id++;
            }
        }
    }

    private void InitVFXAssets()
    {
        string[] folders = new string[] { "" };

        int id = 0;
        foreach (string folder in folders)
        {
            VisualEffectAsset[] vfxAssets = Resources.LoadAll<VisualEffectAsset>(vfxDirectory + folder);
            foreach (VisualEffectAsset asset in vfxAssets)
            {
                cachedVFX.Add(asset.name, asset);
                cachedVFXById.Add(id, asset);
                vfxInstances.Add(asset.name, id);
                id++;
            }
        }
    }

    public AudioClip GetSFXClipByName(string name)
    {
        if (cachedSFX.TryGetValue(name, out AudioClip clip))
        {
            return clip;
        }
        Debug.LogError("FXManager could not find AudioClip with name '" + name + "'");
        return null;
    }

    public int GetSFXIdByName(string name)
    {
        if (sfxInstances.TryGetValue(name, out int id))
        {
            return id;
        }
        Debug.LogError("FXManager could not find AudioClip with name '" + name + "'");
        return -1;
    }

    public AudioClip GetSFXClipById(int id)
    {
        if (cachedSFXById.TryGetValue(id, out AudioClip clip))
        {
            return clip;
        }
        Debug.LogError("FXManager could not find AudioClip with ID '" + id + "'");
        return null;
    }

    public VisualEffectAsset GetVFXAssetByName(string name)
    {
        if (cachedVFX.TryGetValue(name, out VisualEffectAsset asset))
        {
            return asset;
        }
        Debug.LogError("FXManager could not find VisualEffectAsset with name '" + name + "'");
        return null;
    }

    public VisualEffectAsset GetVFXAssetById(int id)
    {
        if (cachedVFXById.TryGetValue(id, out VisualEffectAsset asset))
        {
            return asset;
        }
        Debug.LogError("FXManager could not find VisualEffectAsset with ID '" + id + "'");
        return null;
    }

    public int GetVFXIdByName(string name)
    {
        if (vfxInstances.TryGetValue(name, out int id))
        {
            return id;
        }
        Debug.LogError("FXManager could not find VisualEffectAsset with name '" + name + "'");
        return -1;
    }
}

public enum GameMode
{
    Singleplayer,
    Multiplayer,
}
