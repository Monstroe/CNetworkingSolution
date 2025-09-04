using UnityEngine;

public class ClientItem : ClientInteractable
{
    public ItemType Type { get => startingItemType; set => startingItemType = value; }

    [SerializeField] private ItemType startingItemType = ItemType.NONE;

    public override void Grab(ClientPlayer interactingPlayer, NetPacket packet, TransportMethod? transportMethod)
    {
        base.Grab(interactingPlayer, packet, transportMethod);
        Debug.Log("Object with Id " + interactingPlayer.Id + " grabbed object");
        // Additional logic for when an item is grabbed can be added here
    }

    public override void Interact(ClientPlayer interactingPlayer, NetPacket packet, TransportMethod? transportMethod)
    {
        base.Interact(interactingPlayer, packet, transportMethod);
        Debug.Log("Object with Id " + interactingPlayer.Id + " interacted with object");
        // Additional logic for when an item is interacted with can be added here
    }

    public override void Drop(ClientPlayer interactingPlayer, NetPacket packet, TransportMethod? transportMethod)
    {
        base.Drop(interactingPlayer, packet, transportMethod);
        Debug.Log("Object with Id " + interactingPlayer.Id + " dropped object");
        // Additional logic for when an item is dropped can be added here
    }
}
