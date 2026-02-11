using System.Net;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ServerTransform : ServerObject
{
    public Rigidbody RB { get; private set; }

    [Header("ServerTransform Settings")]
    [Range(0f, 1f)]
    [Tooltip("Speed at which this object syncs it's position and rotation (relative to FixedUpdate)")]
    [SerializeField] private float transformSyncSpeed = 1f;

    private Vector3 receivedPosition;
    private Quaternion receivedRotation;

    private float timer = 0f;

    public override void Init(ushort id, ServerLobby lobby)
    {
        base.Init(id, lobby);
        RB = GetComponent<Rigidbody>();
        this.receivedPosition = RB.position;
        this.receivedRotation = RB.rotation;
        lobby.GetService<ObjectServerService>().ServerTransforms.Add(id, this);
    }

    public override void Remove()
    {
        base.Remove();
        lobby.GetService<ObjectServerService>().ServerTransforms.Remove(Id);
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
            ReceiveTransform(receivedPosition, receivedRotation);
        }
        else
        {
            receivedPosition = RB.position;
            receivedRotation = RB.rotation;
        }

        timer += transformSyncSpeed;
        if (timer >= 1)
        {
            timer = 0;
            SendToGameClientObjects(PacketBuilder.ObjectTransform(RB.position, RB.rotation), TransportMethod.Unreliable, Owner != null ? Owner.User : null);
        }
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

    protected virtual void ReceiveTransform(Vector3 pos, Quaternion rot)
    {
        RB.MovePosition(pos);
        RB.MoveRotation(rot.normalized);
        Debug.Log($"[SERVER] Received transform {pos},{rot}");
    }
}
