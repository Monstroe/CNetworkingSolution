using UnityEngine;

public abstract class ServerInteractable : ServerObject
{
    public ServerPlayer InteractingPlayer { get; set; }

#nullable enable
    public virtual void Grab(ServerPlayer interactingPlayer, ServerLobby lobby, UserData user, NetPacket? packet, TransportMethod? transportMethod)
    {
        interactingPlayer.CurrentInteractable = this;
        InteractingPlayer = interactingPlayer;
    }

    public virtual void Interact(ServerPlayer interactingPlayer, ServerLobby lobby, UserData user, NetPacket? packet, TransportMethod? transportMethod)
    {

    }

    public virtual void Drop(ServerPlayer interactingPlayer, ServerLobby lobby, UserData user, NetPacket? packet, TransportMethod? transportMethod)
    {
        interactingPlayer.CurrentInteractable = null;
        InteractingPlayer = null;
    }
#nullable disable

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.INTERACTABLE_GRAB:
                {
                    lobby.GameData.ServerPlayers.TryGetValue(user, out ServerPlayer player);
                    if (player != null && player.CurrentInteractable == null && InteractingPlayer == null)
                    {
                        Grab(player, lobby, user, packet, transportMethod);
                        lobby.SendToGame(PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerGrab(user.PlayerId)), transportMethod ?? TransportMethod.Reliable);
                    }
                    break;
                }
            case CommandType.INTERACTABLE_INTERACT:
                {
                    lobby.GameData.ServerPlayers.TryGetValue(user, out ServerPlayer player);
                    if (player != null && player.CurrentInteractable == this && InteractingPlayer == player)
                    {
                        Interact(player, lobby, user, packet, transportMethod);
                        lobby.SendToGame(PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerInteract(user.PlayerId)), transportMethod ?? TransportMethod.Reliable);
                    }
                    break;
                }
            case CommandType.INTERACTABLE_DROP:
                {
                    lobby.GameData.ServerPlayers.TryGetValue(user, out ServerPlayer player);
                    if (player != null && player.CurrentInteractable == this && InteractingPlayer == player)
                    {
                        Drop(player, lobby, user, packet, transportMethod);
                        lobby.SendToGame(PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerDrop(user.PlayerId)), transportMethod ?? TransportMethod.Reliable);
                    }
                    break;
                }
        }
    }
}
