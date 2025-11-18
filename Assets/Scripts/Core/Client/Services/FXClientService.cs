using System.Net;
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
                    string sfxPath = NetResources.Instance.GetSFXPathFromKey(sfxId);
                    if (sfxPath != null)
                    {
                        FXManager.Instance.PlaySFX(sfxPath, volume, pos, false);
                    }
                    break;
                }
            case CommandType.VFX:
                {
                    int vfxId = packet.ReadInt();
                    Vector3 pos = packet.ReadVector3();
                    float scale = packet.ReadFloat();
                    string vfxPath = NetResources.Instance.GetVFXPathFromKey(vfxId);
                    if (vfxPath != null)
                    {
                        FXManager.Instance.PlayVFX(vfxPath, pos, scale, false);
                    }
                    break;
                }
        }
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }
}
