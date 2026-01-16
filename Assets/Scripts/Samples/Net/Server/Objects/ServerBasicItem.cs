using UnityEngine;

public class ServerBasicItem : ServerInteractable
{
#nullable enable
    public override void Grab(ServerPlayer interactingPlayer, UserData user, NetPacket? packet, TransportMethod? transportMethod)
    {
        base.Grab(interactingPlayer, user, packet, transportMethod);
        Debug.Log("Player " + user.Settings.UserName + " grabbed basic item on server");
    }

    public override void Interact(ServerPlayer interactingPlayer, UserData user, NetPacket? packet, TransportMethod? transportMethod)
    {
        base.Interact(interactingPlayer, user, packet, transportMethod);
        Debug.Log("Player " + user.Settings.UserName + " interacted with basic item on server");
    }

    public override void Drop(ServerPlayer interactingPlayer, UserData user, NetPacket? packet, TransportMethod? transportMethod)
    {
        base.Drop(interactingPlayer, user, packet, transportMethod);
        Debug.Log("Player " + user.Settings.UserName + " dropped basic item on server");
    }
#nullable disable
}
