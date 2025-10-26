using UnityEngine;

public class ClientBasicItem : ClientInteractable
{
    public override void GrabOnOwner(NetPacket packet, TransportMethod? transportMethod)
    {
        base.GrabOnOwner(packet, transportMethod);
        Debug.Log("Current player grabbed basic item");
    }

    public override void GrabOnNonOwner(OtherPlayer otherPlayer, NetPacket packet, TransportMethod? transportMethod)
    {
        base.GrabOnNonOwner(otherPlayer, packet, transportMethod);
        Debug.Log("Other player with Id " + otherPlayer.Id + " grabbed basic item");
    }

    public override void InteractOnOwner(NetPacket packet, TransportMethod? transportMethod)
    {
        base.InteractOnOwner(packet, transportMethod);
        Debug.Log("Current player interacted with basic item");
    }

    public override void InteractOnNonOwner(OtherPlayer otherPlayer, NetPacket packet, TransportMethod? transportMethod)
    {
        base.InteractOnNonOwner(otherPlayer, packet, transportMethod);
        Debug.Log("Other player with Id " + otherPlayer.Id + " interacted with basic item");
    }

    public override void DropOnOwner(NetPacket packet, TransportMethod? transportMethod)
    {
        base.DropOnOwner(packet, transportMethod);
        Debug.Log("Current player dropped basic item");
    }

    public override void DropOnNonOwner(OtherPlayer otherPlayer, NetPacket packet, TransportMethod? transportMethod)
    {
        base.DropOnNonOwner(otherPlayer, packet, transportMethod);
        Debug.Log("Other player with Id " + otherPlayer.Id + " dropped basic item");
    }

    protected override void UpdateOnOwner()
    {
        base.UpdateOnOwner();
        Debug.Log("[UPDATE] Current player is holding basic item");
    }

    protected override void UpdateOnNonOwner()
    {
        base.UpdateOnNonOwner();
        if (Owner != null)
            Debug.Log("[UPDATE] Other player with Id " + Owner.Id + " is holding basic item");
    }

    protected override void FixedUpdateOnOwner()
    {
        base.FixedUpdateOnOwner();
        Debug.Log("[FIXED UPDATE] Current player is holding basic item");
    }

    protected override void FixedUpdateOnNonOwner()
    {
        base.FixedUpdateOnNonOwner();
        if (Owner != null)
            Debug.Log("[FIXED UPDATE] Other player with Id " + Owner.Id + " is holding basic item");
    }
}
