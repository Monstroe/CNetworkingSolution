using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class FXManager : MonoBehaviour
{
    public static FXManager Instance { get; private set; }

    [Header("SFX Details")]
    [SerializeField] private GameObject sfxPrefab;
    [SerializeField] private string sfxDirectory = "SFX/";

    [Header("VFX Details")]
    [SerializeField] private GameObject vfxPrefab;
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
            Debug.LogWarning("Multiple instances of FXManager detected. Destroying duplicate instance.");
            Destroy(gameObject);
        }

        InitSFXClips();
        InitVFXAssets();
    }

    private void InitSFXClips()
    {
        string[] folders = new string[] { sfxDirectory };

        int id = 0;
        foreach (string folder in folders)
        {
            AudioClip[] clips = Resources.LoadAll<AudioClip>(folder);
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
        string[] folders = new string[] { vfxDirectory };

        int id = 0;
        foreach (string folder in folders)
        {
            VisualEffectAsset[] vfxAssets = Resources.LoadAll<VisualEffectAsset>(folder);
            foreach (VisualEffectAsset asset in vfxAssets)
            {
                cachedVFX.Add(asset.name, asset);
                cachedVFXById.Add(asset.GetInstanceID(), asset);
                vfxInstances.Add(asset.name, asset.GetInstanceID());
                id++;
            }
        }
    }

    public AudioSource PlaySFX(string name, float volume, Vector3? pos = null, bool sync = true)
    {
        AudioSource newSFX = Instantiate(sfxPrefab).GetComponent<AudioSource>();
        newSFX.clip = GetSFXByName(name);
        newSFX.volume = volume;

        if (newSFX.clip == null)
        {
            Debug.LogError("FXManager could not find AudioClip with name '" + name + "'");
            return null;
        }

        if (pos != null)
        {
            newSFX.transform.position = (Vector3)pos;
            newSFX.spatialBlend = 1f;
        }

        newSFX.Play();
        Destroy(newSFX.gameObject, newSFX.clip.length);

        if (sync)
        {
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.PlaySFX(sfxInstances[name], volume, pos), TransportMethod.ReliableUnordered);
        }

        return newSFX;
    }

    public AudioSource PlaySFX(AudioClip clip, float volume, Vector3? pos = null, bool sync = true)
    {
        AudioSource newSFX = Instantiate(sfxPrefab).GetComponent<AudioSource>();
        newSFX.clip = clip;
        newSFX.volume = volume;

        if (newSFX.clip == null)
        {
            Debug.LogError("FXManager could not find AudioClip with ID '" + clip.GetInstanceID() + "'");
            return null;
        }

        if (pos != null)
        {
            newSFX.transform.position = (Vector3)pos;
            newSFX.spatialBlend = 1f;
        }

        newSFX.Play();
        Destroy(newSFX.gameObject, newSFX.clip.length);

        if (sync)
        {
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.PlaySFX(sfxInstances[clip.name], volume, pos), TransportMethod.ReliableUnordered);
        }

        return newSFX;
    }

    public VisualEffect PlayVFX(string name, Vector3 position, float scale, bool sync = true)
    {
        VisualEffect vfx = Instantiate(vfxPrefab, position, Quaternion.identity).GetComponent<VisualEffect>();
        vfx.visualEffectAsset = GetVFXByName(name);
        vfx.transform.localScale = new Vector3(scale, scale, scale);

        if (vfx.visualEffectAsset == null)
        {
            Debug.LogError("FXManager could not find VisualEffectAsset with name '" + name + "'");
            return null;
        }

        if (vfx.HasFloat("_Duration"))
        {
            Destroy(vfx.gameObject, vfx.GetFloat("_Duration"));
        }
        else
        {
            Debug.LogWarning("FXManager could not find a _Duration property for VisualEffectAsset with name '" + name + "', will not be destroyed!");
        }

        if (sync)
        {
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.PlayVFX(vfxInstances[name], position, scale), TransportMethod.ReliableUnordered);
        }

        return vfx;
    }

    public void PlayVFX(VisualEffectAsset asset, Vector3 position, float scale, bool sync = true)
    {
        VisualEffect vfx = Instantiate(vfxPrefab, position, Quaternion.identity).GetComponent<VisualEffect>();
        vfx.visualEffectAsset = asset;
        vfx.transform.localScale = new Vector3(scale, scale, scale);

        if (vfx.visualEffectAsset == null)
        {
            Debug.LogError("FXManager could not find VisualEffectAsset with ID '" + asset.GetInstanceID() + "'");
            return;
        }

        if (vfx.HasFloat("_Duration"))
        {
            Destroy(vfx.gameObject, vfx.GetFloat("_Duration"));
        }
        else
        {
            Debug.LogWarning("FXManager could not find a _Duration property for VisualEffectAsset with ID '" + asset.GetInstanceID() + "', will not be destroyed!");
        }

        if (sync)
        {
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.PlayVFX(vfxInstances[asset.name], position, scale), TransportMethod.ReliableUnordered);
        }
    }

    public AudioClip GetSFXByName(string name)
    {
        if (cachedSFX.TryGetValue(name, out AudioClip clip))
        {
            return clip;
        }
        Debug.LogError("FXManager could not find AudioClip with name '" + name + "'");
        return null;
    }

    public AudioClip GetSFXById(int id)
    {
        if (cachedSFXById.TryGetValue(id, out AudioClip clip))
        {
            return clip;
        }
        Debug.LogError("FXManager could not find AudioClip with ID '" + id + "'");
        return null;
    }

    public VisualEffectAsset GetVFXByName(string name)
    {
        if (cachedVFX.TryGetValue(name, out VisualEffectAsset asset))
        {
            return asset;
        }
        Debug.LogError("FXManager could not find VisualEffectAsset with name '" + name + "'");
        return null;
    }

    public VisualEffectAsset GetVFXById(int id)
    {
        if (cachedVFXById.TryGetValue(id, out VisualEffectAsset asset))
        {
            return asset;
        }
        Debug.LogError("FXManager could not find VisualEffectAsset with ID '" + id + "'");
        return null;
    }
}