using System.Net;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ServerTransform : ServerObject
{
    public Rigidbody RB { get; private set; }

    private Vector3 receivedPosition;
    private Quaternion receivedRotation;

    public override void Init(ushort id, ServerLobby lobby)
    {
        base.Init(id, lobby);
        RB = GetComponent<Rigidbody>();
        this.receivedPosition = RB.position;
        this.receivedRotation = RB.rotation;
    }

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_TRANSFORM:
                {
                    if (Owner != null && Owner.User == user)
                    {
                        receivedPosition = packet.ReadVector3();
                        receivedRotation = packet.ReadQuaternion();
                    }
                    break;
                }
        }
    }

    public override void Tick()
    {
        if (Owner != null)
        {
            SyncPosition(receivedPosition);
            SyncRotation(receivedRotation);
        }
        else
        {
            receivedPosition = RB.position;
            receivedRotation = RB.rotation;
        }

        SendToGameClientObject(PacketBuilder.ObjectTransform(RB.position, RB.rotation), TransportMethod.Unreliable, Owner != null ? Owner.User : null);
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
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

    protected virtual void SyncPosition(Vector3 pos)
    {
        RB.MovePosition(pos);
    }

    protected virtual void SyncRotation(Quaternion rot)
    {
        RB.MoveRotation(rot.normalized);
    }
}
