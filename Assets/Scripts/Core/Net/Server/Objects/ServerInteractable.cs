using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ServerInteractable : ServerObject
{
    public Rigidbody RB { get; private set; }

    private Vector3 position;
    private Quaternion rotation;
    private Vector3 forward;

    public override void Init(ushort id, ServerLobby lobby)
    {
        base.Init(id, lobby);
        lobby.GetService<InteractableServerService>().ServerInteractables.Add(id, this);
        RB = GetComponent<Rigidbody>();
        RB.isKinematic = false;
    }

    public override void Remove()
    {
        if (Owner != null)
        {
            Drop(Owner, Owner.User, null, TransportMethod.Reliable);
        }

        lobby.GetService<InteractableServerService>().ServerInteractables.Remove(Id);
        base.Remove();
    }

#nullable enable
    public virtual void Grab(ServerPlayer interactingPlayer, UserData user, NetPacket? packet, TransportMethod? transportMethod)
    {
        SendToGameClientObject(PacketBuilder.InteractableGrab(user.PlayerId), transportMethod ?? TransportMethod.Reliable);
        interactingPlayer.CurrentInteractable = this;
        Owner = interactingPlayer;
        RB.isKinematic = true;
    }

    public virtual void Interact(ServerPlayer interactingPlayer, UserData user, NetPacket? packet, TransportMethod? transportMethod)
    {
        SendToGameClientObject(PacketBuilder.InteractableInteract(user.PlayerId), transportMethod ?? TransportMethod.Reliable);
    }

    public virtual void Drop(ServerPlayer interactingPlayer, UserData user, NetPacket? packet, TransportMethod? transportMethod)
    {
        SendToGameClientObject(PacketBuilder.InteractableDrop(user.PlayerId), transportMethod ?? TransportMethod.Reliable);
        interactingPlayer.CurrentInteractable = null;
        Owner = null;
        RB.isKinematic = false;
    }
#nullable disable

    public override void Tick()
    {
        if (Owner != null)
        {
            RB.MovePosition(position);
            RB.MoveRotation(rotation.normalized);
        }
        else
        {
            position = RB.position;
            rotation = RB.rotation;
        }

        SendToGameClientObject(PacketBuilder.InteractableTransform(RB.position, RB.rotation, forward), TransportMethod.Unreliable, Owner != null ? Owner.User : null);
    }

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.INTERACTABLE_TRANSFORM:
                {
                    if (Owner != null && Owner.User == user)
                    {
                        position = packet.ReadVector3();
                        rotation = packet.ReadQuaternion();
                        forward = packet.ReadVector3();

                        Debug.Log($"Received interactable transform from owner {user.PlayerId} - Pos: {position} Rot: {rotation.eulerAngles} Forward: {forward}");
                    }
                    break;
                }
        }
    }

    public override void UserJoined(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        if (Owner != null)
        {
            SendToUserClientObject(joinedUser, PacketBuilder.InteractableGrab(Owner.User.PlayerId), TransportMethod.Reliable);
        }
    }

    public override void UserLeft(UserData leftUser)
    {
        // Nothing
    }
}
