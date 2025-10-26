using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.VFX;

public class FXManager : MonoBehaviour
{
    public static FXManager Instance { get; private set; }

    [Header("SFX Details")]
    [SerializeField] private GameObject sfxPrefab;

    [Header("VFX Details")]
    [SerializeField] private GameObject vfxPrefab;

    [SerializeField] private string sfxDirectory = "Assets/GameAssets/SFX/";
    [SerializeField] private string vfxDirectory = "Assets/GameAssets/VFX/";

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
            return;
        }
    }

    public void PlaySFX(string name, float volume, Vector3? pos = null, bool sync = true)
    {
        if (sync)
        {
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.PlaySFXRequest(sfxDirectory + name, volume, pos), TransportMethod.Reliable);
        }
        else
        {
            Addressables.LoadAssetAsync<AudioClip>(sfxDirectory + name).Completed += (handle) =>
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
                    Debug.LogError("FXManager could not load AudioClip with name '" + name + "'");
                }
            };
        }
    }

    public void PlayVFX(string name, Vector3 position, float scale, bool sync = true)
    {
        if (sync)
        {
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.PlayVFXRequest(vfxDirectory + name, position, scale), TransportMethod.Reliable);
        }
        else
        {
            Addressables.LoadAssetAsync<VisualEffectAsset>(vfxDirectory + name).Completed += (handle) =>
            {
                VisualEffectAsset asset = handle.Result;
                if (asset != null)
                {
                    VisualEffect vfx = Instantiate(vfxPrefab, position, Quaternion.identity).GetComponent<VisualEffect>();
                    vfx.visualEffectAsset = asset;
                    vfx.transform.localScale = new Vector3(scale, scale, scale);

                    if (vfx.HasFloat("_Duration"))
                    {
                        Destroy(vfx.gameObject, vfx.GetFloat("_Duration"));
                    }
                    else
                    {
                        Debug.LogWarning("FXManager could not find a _Duration property for VisualEffectAsset with name '" + asset.name + "', will not be destroyed!");
                    }
                }
                else
                {
                    Debug.LogError("FXManager could not load VisualEffectAsset with name '" + name + "'");
                }
            };
        }
    }
}
