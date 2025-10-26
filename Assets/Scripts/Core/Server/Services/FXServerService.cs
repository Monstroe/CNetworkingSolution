using UnityEngine;
using UnityEngine.VFX;

public class FXServerService : ServerService
{
    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.SFX:
                {
                    int sfxKey = packet.ReadInt();
                    float volume = packet.ReadFloat();
                    Vector3? pos = packet.UnreadLength > 0 ? packet.ReadVector3() : null;
                    if (NetResources.Instance.GetSFXPathFromKey(sfxKey) != null)
                    {
                        lobby.SendToGame(PacketBuilder.PlaySFX(sfxKey, volume, pos), transportMethod ?? TransportMethod.Reliable, user);
                    }
                    break;
                }
            case CommandType.VFX:
                {
                    int vfxId = packet.ReadInt();
                    Vector3 pos = packet.ReadVector3();
                    float scale = packet.ReadFloat();
                    if (NetResources.Instance.GetVFXPathFromKey(vfxId) != null)
                    {
                        lobby.SendToGame(PacketBuilder.PlayVFX(vfxId, pos, scale), transportMethod ?? TransportMethod.Reliable, user);
                    }
                    break;
                }
        }
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
