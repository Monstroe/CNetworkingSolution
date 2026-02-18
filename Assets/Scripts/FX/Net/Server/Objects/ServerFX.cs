using UnityEngine;

public class ServerFX : ServerObject
{
    [Header("Directories")]
    [SerializeField] private string sfxDirectory = "Assets/GameAssets/SFX/";
    [SerializeField] private string vfxDirectory = "Assets/GameAssets/VFX/";

    public override void Init(ushort id, ServerLobby lobby)
    {
        base.Init(id, lobby);
        lobby.GetService<FXServerService>().SetFX(this);
    }

    public void PlaySFX(string name, float volume, Vector3? pos = null)
    {
        int key = NetResources.Instance.GetSFXKeyFromPath(sfxDirectory + name);
        if (key == 0)
        {
            Debug.LogError("PacketBuilder PlaySFXRequest could not find SFX key for path: " + sfxDirectory + name);
        }
        InvokeOnGameClientObjects(nameof(PlaySFXRpc), key, volume, pos);
    }

    [Rpc]
    private void PlaySFXRpc(int key, float volume, Vector3? pos = null)
    {
        InvokeOnGameClientObjects(nameof(PlaySFXRpc), key, volume, pos);
    }

    public void PlayVFX(string name, Vector3 pos, float scale)
    {
        int key = NetResources.Instance.GetVFXKeyFromPath(vfxDirectory + name);
        if (key == 0)
        {
            Debug.LogError("PacketBuilder PlayVFXRequest could not find VFX key for path: " + vfxDirectory + name);
        }
        InvokeOnGameClientObjects(nameof(PlayVFXRpc), key, pos, scale);
    }

    [Rpc]
    private void PlayVFXRpc(int key, Vector3 pos, float scale)
    {
        InvokeOnGameClientObjects(nameof(PlayVFXRpc), key, pos, scale);
    }

    public override void Tick()
    {
        // Nothing
    }

    public override void UserJoined(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserLeft(UserData leftUser)
    {
        // Nothing
    }
}
