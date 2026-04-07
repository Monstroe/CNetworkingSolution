using UnityEngine;
using CNetworkingSolution;

[ServiceId("FXService")]
public class FXServerService : ServerService
{
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
        InvokeOnGameClientServices(nameof(PlaySFXRpc), key, volume, pos);
    }

    [ServerRpc]
    private async void PlaySFXRpc(ulong key, float volume, Vector3? pos = null)
    {
        SFXRequestReceivedEvent evt = new SFXRequestReceivedEvent()
        {
            SFXKey = key,
            Volume = volume,
            Position = pos
        };
        var result = await lobby.TriggerGameEvent(evt);
        if (!result.Canceled)
        {
            InvokeOnGameClientServices(nameof(PlaySFXRpc), key, volume, pos);
        }
    }

    public void PlayVFX(string name, Vector3 pos, float scale)
    {
        ulong key = NetResources.Instance.GetVFXKeyFromPath(vfxDirectory + name);
        if (key == 0)
        {
            Debug.LogError("PacketBuilder PlayVFXRequest could not find VFX key for path: " + vfxDirectory + name);
        }
        InvokeOnGameClientServices(nameof(PlayVFXRpc), key, pos, scale);
    }

    [ServerRpc]
    private async void PlayVFXRpc(ulong key, Vector3 pos, float scale)
    {
        VFXRequestReceivedEvent evt = new VFXRequestReceivedEvent()
        {
            VFXKey = key,
            Position = pos,
            Scale = scale
        };
        var result = await lobby.TriggerGameEvent(evt);
        if (!result.Canceled)
        {
            InvokeOnGameClientServices(nameof(PlayVFXRpc), key, pos, scale);
        }
    }
}

public class SFXRequestReceivedEvent : GameEvent
{
    public ulong SFXKey { get; set; }
    public float Volume { get; set; }
    public Vector3? Position { get; set; }
}

public class VFXRequestReceivedEvent : GameEvent
{
    public ulong VFXKey { get; set; }
    public Vector3 Position { get; set; }
    public float Scale { get; set; }
}
