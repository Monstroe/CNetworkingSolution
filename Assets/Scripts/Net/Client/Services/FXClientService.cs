using UnityEngine;

public class FXClientService : ClientService
{
    void Start()
    {
        ClientManager.Instance.CurrentLobby.RegisterService(ServiceType.FX, this);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.SFX:
                {
                    int sfxId = packet.ReadInt();
                    float volume = packet.ReadFloat();
                    Vector3? pos = packet.UnreadLength > 0 ? packet.ReadVector3() : null;
                    FXManager.Instance.PlaySFX(GameResources.Instance.GetSFXClipById(sfxId), volume, pos, false);
                    break;
                }
            case CommandType.VFX:
                {
                    int vfxId = packet.ReadInt();
                    Vector3 pos = packet.ReadVector3();
                    float scale = packet.ReadFloat();
                    FXManager.Instance.PlayVFX(GameResources.Instance.GetVFXAssetById(vfxId), pos, scale, false);
                    break;
                }
        }
    }
}
