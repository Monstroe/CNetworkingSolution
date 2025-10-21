using UnityEngine;

public class ServerItem : ServerInteractable
{
    public ItemType Type { get; set; } = ItemType.None;

    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }

    public ServerItem(ushort id, ServerLobby lobby) : base(id, lobby)
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

    public override void Tick()
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

public enum ItemType
{
    None,
    Basic,
    // ADD MORE ITEM TYPES HERE
}
