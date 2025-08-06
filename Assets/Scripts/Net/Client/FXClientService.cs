using UnityEngine;

public class FXClientService : ClientService
{
    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.SFX:
                {
                    int sfxId = packet.ReadInt();
                    float volume = packet.ReadFloat();
                    Vector3? pos = packet.UnreadLength > 0 ? packet.ReadVector3() : null;

                    FXManager.Instance.PlaySFX(FXManager.Instance.GetSFXById(sfxId), volume, pos, false);
                    break;
                }
            case CommandType.VFX:
                {
                    int vfxId = packet.ReadInt();
                    Vector3 pos = packet.ReadVector3();
                    float scale = packet.ReadFloat();
                    FXManager.Instance.PlayVFX(FXManager.Instance.GetVFXById(vfxId), pos, scale, false);
                    break;
                }
        }
    }
}
