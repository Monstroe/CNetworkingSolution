using UnityEngine;

public class ServerInteractable : ServerObject
{
    public ServerObject InteractingObject { get; private set; }

    public ServerInteractable(ushort id) : base(id)
    {
    }

    public virtual void Grab(ServerLobby lobby, UserData user, NetPacket packet, TransportMethod? transportMethod) { }

    public virtual void Drop(ServerLobby lobby, UserData user, NetPacket packet, TransportMethod? transportMethod) { }

    public virtual void Interact(ServerLobby lobby, UserData user, NetPacket packet, TransportMethod? transportMethod) { }

    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.PLAYER_GRAB:
                {
                    ServerPlayer player = CheckPlayerValidity(lobby, user);
                    if (player != null && player.CurrentInteractable == null && InteractingObject == null)
                    {
                        player.CurrentInteractable = this;
                        InteractingObject = player;
                        Grab(lobby, user, packet, transportMethod);
                        lobby.SendToLobby(PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerGrab(user.PlayerId)), transportMethod ?? TransportMethod.Reliable);
                    }
                    break;
                }
            case CommandType.PLAYER_DROP:
                {
                    ServerPlayer player = CheckPlayerValidity(lobby, user);
                    if (player != null && player.CurrentInteractable == this && InteractingObject == player)
                    {
                        Drop(lobby, user, packet, transportMethod);
                        player.CurrentInteractable = null;
                        InteractingObject = null;
                        lobby.SendToLobby(PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerDrop(user.PlayerId)), transportMethod ?? TransportMethod.Reliable);
                    }
                    break;
                }
            case CommandType.PLAYER_INTERACT:
                {
                    ServerPlayer player = CheckPlayerValidity(lobby, user);
                    if (player != null && player.CurrentInteractable == this && InteractingObject == player)
                    {
                        Interact(lobby, user, packet, transportMethod);
                        lobby.SendToLobby(PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerInteract(user.PlayerId)), transportMethod ?? TransportMethod.Reliable);
                    }
                    break;
                }
        }
    }

    private ServerPlayer CheckPlayerValidity(ServerLobby lobby, UserData user)
    {
        lobby.GameData.ServerPlayers.TryGetValue(user, out ServerPlayer serverPlayer);
        if (serverPlayer == null)
        {
            Debug.LogWarning($"ServerPlayer for UserData with PlayerId {user.PlayerId} not found.");
            return null;
        }

        return serverPlayer;
    }

    public override void Tick(ServerLobby lobby)
    {
        // Do nothing here, as this class is meant to be extended
    }
}
