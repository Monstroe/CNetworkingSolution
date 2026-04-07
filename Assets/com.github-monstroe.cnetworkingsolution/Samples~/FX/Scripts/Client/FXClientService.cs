using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.VFX;
using CNetworkingSolution;

[ServiceId("FXService")]
public class FXClientService : ClientService
{
    public delegate void SFXReceivedEventHandler(ulong sfxKey, float volume, Vector3? position);
    public event SFXReceivedEventHandler OnSFXReceived;

    public delegate void VFXReceivedEventHandler(ulong vfxKey, Vector3 position, float scale);
    public event VFXReceivedEventHandler OnVFXReceived;

    [Header("FX Prefabs")]
    [SerializeField] private GameObject sfxPrefab;
    [SerializeField] private GameObject vfxPrefab;

    [Header("FX Directories")]
    [SerializeField] private string sfxDirectory = "Assets/FX/SFX/";
    [SerializeField] private string vfxDirectory = "Assets/FX/VFX/";

    public void PlaySFX(string name, float volume, Vector3? pos = null)
    {
        ulong key = NetResources.Instance.GetSFXKeyFromPath(sfxDirectory + name);
        if (key == 0)
        {
            Debug.LogError("PacketBuilder PlaySFXRequest could not find SFX key for path: " + sfxDirectory + name);
        }
        InvokeOnServerService(nameof(PlaySFXRpc), key, volume, pos);
    }

    [ClientRpc]
    private void PlaySFXRpc(ulong key, float volume, Vector3? pos = null)
    {
        string sfxPath = NetResources.Instance.GetSFXPathFromKey(key);
        if (sfxPath != null)
        {
            Addressables.LoadAssetAsync<AudioClip>(sfxPath).Completed += (handle) =>
            {
                AudioSource sfx = Instantiate(sfxPrefab).GetComponent<AudioSource>();
                AudioClip clip = handle.Result;
                if (clip != null)
                {
                    sfx.clip = clip;
                    sfx.volume = volume;

                    if (pos != null)
                    {
                        sfx.transform.position = (Vector3)pos;
                        sfx.spatialBlend = 1f;
                    }

                    sfx.Play();
                    Destroy(sfx.gameObject, sfx.clip.length);

                    OnSFXReceived?.Invoke(key, volume, pos);
                }
                else
                {
                    Debug.LogError("ClientFX PlaySFXRpc could not load AudioClip with name '" + sfxPath + "'");
                }
            };
        }
        else
        {
            Debug.LogError("ClientFX PlaySFXRpc could not find SFX path for key: " + key);
        }
    }

    public void PlayVFX(string name, Vector3 pos, float scale)
    {
        ulong key = NetResources.Instance.GetVFXKeyFromPath(vfxDirectory + name);
        if (key == 0)
        {
            Debug.LogError("PacketBuilder PlayVFXRequest could not find VFX key for path: " + vfxDirectory + name);
        }
        InvokeOnServerService(nameof(PlayVFXRpc), key, pos, scale);
    }


    [ClientRpc]
    private void PlayVFXRpc(ulong key, Vector3 pos, float scale)
    {
        string vfxPath = NetResources.Instance.GetVFXPathFromKey(key);
        if (vfxPath != null)
        {
            Addressables.LoadAssetAsync<VisualEffectAsset>(vfxPath).Completed += (handle) =>
            {
                VisualEffectAsset asset = handle.Result;
                if (asset != null)
                {
                    VisualEffect vfx = Instantiate(vfxPrefab, pos, Quaternion.identity).GetComponent<VisualEffect>();
                    vfx.visualEffectAsset = asset;
                    vfx.transform.localScale = new Vector3(scale, scale, scale);

                    if (vfx.HasFloat("_Duration"))
                    {
                        Destroy(vfx.gameObject, vfx.GetFloat("_Duration"));
                    }
                    else
                    {
                        Debug.LogWarning("ClientFX PlayVFXRpc could not find a _Duration property for VisualEffectAsset with name '" + asset.name + "', will not be destroyed!");
                    }

                    OnVFXReceived?.Invoke(key, pos, scale);
                }
                else
                {
                    Debug.LogError("ClientFX PlayVFXRpc could not load VisualEffectAsset with name '" + vfxPath + "'");
                }
            };
        }
        else
        {
            Debug.LogError("ClientFX PlayVFXRpc could not find VFX path for key: " + key);
        }
    }
}
