using UnityEngine;

public abstract class ServerInteractable : ServerObject
{
    public ServerObject InteractingObject { get; set; }

    public ServerInteractable(ushort id) : base(id)
    {
    }

#nullable enable
    public abstract void Grab(ServerLobby lobby, UserData user, NetPacket? packet, TransportMethod? transportMethod);

    public abstract void Drop(ServerLobby lobby, UserData user, NetPacket? packet, TransportMethod? transportMethod);

    public abstract void Interact(ServerLobby lobby, UserData user, NetPacket? packet, TransportMethod? transportMethod);
#nullable disable

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
                        lobby.SendToGame(PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerGrab(user.PlayerId)), transportMethod ?? TransportMethod.Reliable);
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
                        lobby.SendToGame(PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerDrop(user.PlayerId)), transportMethod ?? TransportMethod.Reliable);
                    }
                    break;
                }
            case CommandType.PLAYER_INTERACT:
                {
                    ServerPlayer player = CheckPlayerValidity(lobby, user);
                    if (player != null && player.CurrentInteractable == this && InteractingObject == player)
                    {
                        Interact(lobby, user, packet, transportMethod);
                        lobby.SendToGame(PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerInteract(user.PlayerId)), transportMethod ?? TransportMethod.Reliable);
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
