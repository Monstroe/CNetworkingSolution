using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.VFX;

public class ClientFX : ClientObject
{
    [Header("Prefabs")]
    [SerializeField] private GameObject sfxPrefab;
    [SerializeField] private GameObject vfxPrefab;

    [Header("Directories")]
    [SerializeField] private string sfxDirectory = "Assets/GameAssets/SFX/";
    [SerializeField] private string vfxDirectory = "Assets/GameAssets/VFX/";

    public override void Init(ushort id, ClientLobby lobby)
    {
        base.Init(id, lobby);
        lobby.GetService<FXClientService>().FX = this;
    }

    public void PlaySFX(string name, float volume, Vector3? pos = null)
    {
        int key = NetResources.Instance.GetSFXKeyFromPath(sfxDirectory + name);
        if (key == 0)
        {
            Debug.LogError("PacketBuilder PlaySFXRequest could not find SFX key for path: " + sfxDirectory + name);
        }
        InvokeOnServerObject(nameof(PlaySFXRpc), key, volume, pos);
    }

    [Rpc]
    private void PlaySFXRpc(int key, float volume, Vector3? pos = null)
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
        int key = NetResources.Instance.GetVFXKeyFromPath(vfxDirectory + name);
        if (key == 0)
        {
            Debug.LogError("PacketBuilder PlayVFXRequest could not find VFX key for path: " + vfxDirectory + name);
        }
        InvokeOnServerObject(nameof(PlayVFXRpc), key, pos, scale);
    }


    [Rpc]
    private void PlayVFXRpc(int key, Vector3 pos, float scale)
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
