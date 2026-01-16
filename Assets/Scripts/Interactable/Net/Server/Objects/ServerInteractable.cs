using System.Net;
using UnityEngine;

public class ServerInteractable : ServerTransform
{
    public override void Init(ushort id, ServerLobby lobby)
    {
        base.Init(id, lobby);
        lobby.GetService<InteractableServerService>().ServerInteractables.Add(id, this);
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

    public override void UserJoinedGame(UserData joinedUser)
    {
        base.UserJoinedGame(joinedUser);

        if (Owner != null)
        {
            SendToUserClientObject(joinedUser, PacketBuilder.InteractableGrab(Owner.User.PlayerId), TransportMethod.Reliable);
        }
    }
}
