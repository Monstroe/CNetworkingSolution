using UnityEngine;

public class ClientInteractable : ClientObject
{
    public ClientObject InteractingObject { get; protected set; }

    public virtual void Grab(NetPacket packet, TransportMethod? transportMethod) { Debug.Log("Object with Id " + InteractingObject.Id + " grabbed object"); }

    public virtual void Interact(NetPacket packet, TransportMethod? transportMethod) { Debug.Log("Object with Id " + InteractingObject.Id + " interacted with object"); }

    public virtual void Drop(NetPacket packet, TransportMethod? transportMethod) { Debug.Log("Object with Id " + InteractingObject.Id + " dropped object"); }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.PLAYER_GRAB:
                {
                    byte playerId = packet.ReadByte();
                    if (playerId == Player.Instance.Id)
                    {
                        Player.Instance.PlayerInteract.SetInteractable(this);
                        InteractingObject = Player.Instance;
                    }
                    else
                    {
                        UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                        ClientManager.Instance.CurrentLobby.GameData.OtherPlayers.TryGetValue(user, out OtherPlayer otherPlayer);
                        otherPlayer.CurrentInteractable = this;
                        InteractingObject = otherPlayer;
                    }

                    Grab(packet, transportMethod);
                    break;
                }
            case CommandType.PLAYER_DROP:
                {
                    byte playerId = packet.ReadByte();
                    Drop(packet, transportMethod);

                    if (playerId == Player.Instance.Id)
                    {
                        Player.Instance.PlayerInteract.ResetInteractable();
                        InteractingObject = null;
                    }
                    else
                    {
                        ((OtherPlayer)InteractingObject).CurrentInteractable = null;
                        InteractingObject = null;
                    }
                    break;
                }
            case CommandType.PLAYER_INTERACT:
                {
                    byte playerId = packet.ReadByte();
                    Interact(packet, transportMethod);
                    break;
                }
        }
    }
}
