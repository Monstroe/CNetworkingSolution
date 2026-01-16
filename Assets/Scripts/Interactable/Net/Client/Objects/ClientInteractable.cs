using System.Net;
using UnityEngine;

public abstract class ClientInteractable : ClientTransform
{
    public override void Init(ushort id, ClientLobby lobby)
    {
        base.Init(id, lobby);
        lobby.GetService<InteractableClientService>().ClientInteractables.Add(id, this);
    }

    public override void Remove()
    {
        lobby.GetService<InteractableClientService>().ClientInteractables.Remove(Id);
        base.Remove();
    }

    public virtual void GrabOnOwner(NetPacket packet, TransportMethod? transportMethod) { }
    public virtual void GrabOnNonOwner(ClientPlayer otherPlayer, NetPacket packet, TransportMethod? transportMethod) { }
    public virtual void Grab(ClientPlayer interactingPlayer, NetPacket packet, TransportMethod? transportMethod)
    {
        interactingPlayer.CurrentInteractable = this;
        Owner = interactingPlayer;

        if (interactingPlayer == Player.Instance)
        {
            GrabOnOwner(packet, transportMethod);
        }
        else
        {
            GrabOnNonOwner(interactingPlayer, packet, transportMethod);
        }
    }


    public virtual void InteractOnOwner(NetPacket packet, TransportMethod? transportMethod) { }
    public virtual void InteractOnNonOwner(ClientPlayer otherPlayer, NetPacket packet, TransportMethod? transportMethod) { }
    public virtual void Interact(ClientPlayer interactingPlayer, NetPacket packet, TransportMethod? transportMethod)
    {
        if (interactingPlayer == Player.Instance)
        {
            InteractOnOwner(packet, transportMethod);
        }
        else
        {
            InteractOnNonOwner(interactingPlayer, packet, transportMethod);
        }
    }

    public virtual void DropOnOwner(NetPacket packet, TransportMethod? transportMethod) { }
    public virtual void DropOnNonOwner(ClientPlayer otherPlayer, NetPacket packet, TransportMethod? transportMethod) { }
    public virtual void Drop(ClientPlayer interactingPlayer, NetPacket packet, TransportMethod? transportMethod)
    {
        interactingPlayer.CurrentInteractable = null;
        Owner = null;

        if (interactingPlayer == Player.Instance)
        {
            DropOnOwner(packet, transportMethod);
        }
        else
        {
            DropOnNonOwner(interactingPlayer, packet, transportMethod);
        }
    }

    protected override void UpdateOnNonOwner()
    {
        base.UpdateOnNonOwner();
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        base.ReceiveData(packet, serviceType, commandType, transportMethod);
        switch (commandType)
        {
            case CommandType.INTERACTABLE_GRAB:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = lobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    lobby.GetService<PlayerClientService>().ClientPlayers.TryGetValue(user, out ClientPlayer clientPlayer);
                    Grab(clientPlayer, packet, transportMethod);
                    break;
                }
            case CommandType.INTERACTABLE_INTERACT:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = lobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    lobby.GetService<PlayerClientService>().ClientPlayers.TryGetValue(user, out ClientPlayer clientPlayer);
                    Interact(clientPlayer, packet, transportMethod);
                    break;
                }
            case CommandType.INTERACTABLE_DROP:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = lobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    lobby.GetService<PlayerClientService>().ClientPlayers.TryGetValue(user, out ClientPlayer clientPlayer);
                    Drop(clientPlayer, packet, transportMethod);
                    break;
                }
        }
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }
}
