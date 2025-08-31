using UnityEngine;

public class ClientItem : ClientInteractable
{
    public ItemType Type { get => startingItemType; set => startingItemType = value; }

    [SerializeField] private ItemType startingItemType = ItemType.NONE;

    public override void Grab(NetPacket packet, TransportMethod? transportMethod)
    {
        Debug.Log("Object with Id " + InteractingObject.Id + " grabbed object");
    }

    public override void Interact(NetPacket packet, TransportMethod? transportMethod)
    {
        Debug.Log("Object with Id " + InteractingObject.Id + " interacted with object");
    }

    public override void Drop(NetPacket packet, TransportMethod? transportMethod)
    {
        Debug.Log("Object with Id " + InteractingObject.Id + " dropped object");
    }
}
