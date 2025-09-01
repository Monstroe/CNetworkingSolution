public abstract class ClientInteractable : ClientObject
{
    public ClientPlayer InteractingPlayer { get; protected set; }

    public virtual void Grab(ClientPlayer interactingPlayer, NetPacket packet, TransportMethod? transportMethod)
    {
        if (interactingPlayer == Player.Instance)
        {
            Player.Instance.PlayerInteract.SetInteractable(this);
        }
        else
        {
            interactingPlayer.CurrentInteractable = this;
        }

        InteractingPlayer = interactingPlayer;
    }

    public virtual void Interact(ClientPlayer interactingPlayer, NetPacket packet, TransportMethod? transportMethod)
    {

    }

    public virtual void Drop(ClientPlayer interactingPlayer, NetPacket packet, TransportMethod? transportMethod)
    {
        if (interactingPlayer == Player.Instance)
        {
            Player.Instance.PlayerInteract.SetInteractable(null);
        }
        else
        {
            interactingPlayer.CurrentInteractable = null;
        }

        InteractingPlayer = null;
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.PLAYER_GRAB:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    ClientManager.Instance.CurrentLobby.GameData.ClientPlayers.TryGetValue(user, out ClientPlayer clientPlayer);
                    Grab(clientPlayer, packet, transportMethod);
                    break;
                }
            case CommandType.PLAYER_INTERACT:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    ClientManager.Instance.CurrentLobby.GameData.ClientPlayers.TryGetValue(user, out ClientPlayer clientPlayer);
                    Interact(clientPlayer, packet, transportMethod);
                    break;
                }
            case CommandType.PLAYER_DROP:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    ClientManager.Instance.CurrentLobby.GameData.ClientPlayers.TryGetValue(user, out ClientPlayer clientPlayer);
                    Drop(clientPlayer, packet, transportMethod);
                    break;
                }
        }
    }
}
