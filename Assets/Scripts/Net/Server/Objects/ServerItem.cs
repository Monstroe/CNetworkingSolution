using UnityEngine;

public class ServerItem : ServerInteractable
{
    public ItemType Type { get; set; } = ItemType.NONE;

    public ServerItem(ushort id) : base(id)
    {
    }

#nullable enable
    public override void Grab(ServerPlayer interactingPlayer, ServerLobby lobby, UserData user, NetPacket? packet, TransportMethod? transportMethod)
    {
        base.Grab(interactingPlayer, lobby, user, packet, transportMethod);
        // Additional logic for when an item is grabbed can be added here
    }

    public override void Interact(ServerPlayer interactingPlayer, ServerLobby lobby, UserData user, NetPacket? packet, TransportMethod? transportMethod)
    {
        base.Interact(interactingPlayer, lobby, user, packet, transportMethod);
        // Additional logic for when an item is interacted with can be added here
    }

    public override void Drop(ServerPlayer interactingPlayer, ServerLobby lobby, UserData user, NetPacket? packet, TransportMethod? transportMethod)
    {
        base.Drop(interactingPlayer, lobby, user, packet, transportMethod);
        // Additional logic for when an item is dropped can be added here
    }
#nullable disable

    public override void Tick(ServerLobby lobby)
    {
        // Nothing
    }

    public override void UserJoined(ServerLobby lobby, UserData joinedUser)
    {
        // Nothing
    }

    public override void UserJoinedGame(ServerLobby lobby, UserData joinedUser)
    {
        // Nothing
    }

    public override void UserLeft(ServerLobby lobby, UserData leftUser)
    {
        // Nothing
    }
}

public enum ItemType
{
    NONE,
    // ADD MORE ITEM TYPES HERE
}
