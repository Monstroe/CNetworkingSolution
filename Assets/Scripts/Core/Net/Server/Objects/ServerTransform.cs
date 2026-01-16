using System.Net;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ServerTransform : ServerObject
{
    public Rigidbody RB { get; private set; }

    protected Vector3 receivedPosition;
    protected Quaternion receivedRotation;
    protected Vector3 receivedForward;

    public override void Init(ushort id, ServerLobby lobby)
    {
        base.Init(id, lobby);
        RB = GetComponent<Rigidbody>();
        this.receivedPosition = RB.position;
        this.receivedRotation = RB.rotation;
        this.receivedForward = RB.transform.forward;
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
                        receivedForward = packet.ReadVector3();
                    }
                    break;
                }
        }
    }

    public override void Tick()
    {
        if (Owner != null)
        {
            RB.MovePosition(receivedPosition);
            RB.MoveRotation(receivedRotation.normalized);
        }
        else
        {
            receivedPosition = RB.position;
            receivedRotation = RB.rotation;
        }

        SendToGameClientObject(PacketBuilder.ObjectTransform(RB.position, RB.rotation, receivedForward), TransportMethod.Unreliable, Owner != null ? Owner.User : null);
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
}
