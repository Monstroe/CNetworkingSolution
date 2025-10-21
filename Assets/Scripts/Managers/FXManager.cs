using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class FXManager : MonoBehaviour
{
    public static FXManager Instance { get; private set; }

    [Header("SFX Details")]
    [SerializeField] private GameObject sfxPrefab;

    [Header("VFX Details")]
    [SerializeField] private GameObject vfxPrefab;

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

    public AudioSource PlaySFX(string name, float volume, Vector3? pos = null, bool sync = true)
    {
        AudioClip clip = GameResources.Instance.GetSFXClipByName(name);
        if (clip == null)
        {
            Debug.LogError("FXManager could not find AudioClip with name '" + name + "'");
            return null;
        }

        return PlaySFX(clip, volume, pos, sync);
    }

    public AudioSource PlaySFX(AudioClip clip, float volume, Vector3? pos = null, bool sync = true)
    {
        AudioSource sfx = Instantiate(sfxPrefab).GetComponent<AudioSource>();
        sfx.clip = clip;
        sfx.volume = volume;

        if (pos != null)
        {
            sfx.transform.position = (Vector3)pos;
            sfx.spatialBlend = 1f;
        }

        sfx.Play();
        Destroy(sfx.gameObject, sfx.clip.length);

        if (sync)
        {
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.PlaySFX(clip.name, volume, pos), TransportMethod.Reliable);
        }

        return sfx;
    }

    public VisualEffect PlayVFX(string name, Vector3 position, float scale, bool sync = true)
    {
        VisualEffectAsset asset = GameResources.Instance.GetVFXAssetByName(name);
        if (asset == null)
        {
            Debug.LogError("FXManager could not find VisualEffectAsset with name '" + name + "'");
            return null;
        }

        return PlayVFX(asset, position, scale, sync);
    }

    public VisualEffect PlayVFX(VisualEffectAsset asset, Vector3 position, float scale, bool sync = true)
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

        if (sync)
        {
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.PlayVFX(asset.name, position, scale), TransportMethod.Reliable);
        }

        return vfx;
    }
}