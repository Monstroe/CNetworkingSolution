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
        lobby.GetService<ObjectServerService>().ServerTransforms.Add(id, this);
    }

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        base.ReceiveData(user, packet, serviceType, commandType, transportMethod);
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
            UpdatePosition(receivedPosition);
            UpdateRotation(receivedRotation);
        }
        else
        {
            receivedPosition = RB.position;
            receivedRotation = RB.rotation;
        }

        SendToGameClientObjects(PacketBuilder.ObjectTransform(RB.position, RB.rotation), TransportMethod.Unreliable, Owner != null ? Owner.User : null);
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

    protected virtual void UpdatePosition(Vector3 pos)
    {
        RB.MovePosition(pos);
    }

    protected virtual void UpdateRotation(Quaternion rot)
    {
        RB.MoveRotation(rot.normalized);
    }
}
