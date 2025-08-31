using UnityEngine;

public class ServerItem : ServerInteractable
{
    public ItemType Type { get; set; } = ItemType.NONE;

    public ServerItem(ushort id) : base(id)
    {
    }

#nullable enable
    public override void Grab(ServerLobby lobby, UserData user, NetPacket? packet, TransportMethod? transportMethod) { }

    public override void Drop(ServerLobby lobby, UserData user, NetPacket? packet, TransportMethod? transportMethod) { }

    public override void Interact(ServerLobby lobby, UserData user, NetPacket? packet, TransportMethod? transportMethod) { }
#nullable disable

}

public enum ItemType
{
    NONE,
    // ADD MORE ITEM TYPES HERE
}
