using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class ClientInteractable : ClientObject
{
    [Header("Movement")]
    [SerializeField] private float lerpSpeed = 15f;

    public Rigidbody RB { get; private set; }

    // Movement
    private Vector3 position;
    private Quaternion rotation;
    private Vector3 forward;

    private bool firstTransformReceived = false;

    public override void Init(ushort id)
    {
        base.Init(id);
        ClientManager.Instance.CurrentLobby.GameData.ClientInteractables.Add(id, this);
        RB = GetComponent<Rigidbody>();
    }

    public override void Remove()
    {
        ClientManager.Instance.CurrentLobby.GameData.ClientInteractables.Remove(Id);
        base.Remove();
    }

    public virtual void GrabOnOwner(NetPacket packet, TransportMethod? transportMethod) { }
    public virtual void GrabOnNonOwner(OtherPlayer otherPlayer, NetPacket packet, TransportMethod? transportMethod) { }
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
            GrabOnNonOwner((OtherPlayer)interactingPlayer, packet, transportMethod);
        }
    }


    public virtual void InteractOnOwner(NetPacket packet, TransportMethod? transportMethod) { }
    public virtual void InteractOnNonOwner(OtherPlayer otherPlayer, NetPacket packet, TransportMethod? transportMethod) { }
    public virtual void Interact(ClientPlayer interactingPlayer, NetPacket packet, TransportMethod? transportMethod)
    {
        if (interactingPlayer == Player.Instance)
        {
            InteractOnOwner(packet, transportMethod);
        }
        else
        {
            InteractOnNonOwner((OtherPlayer)interactingPlayer, packet, transportMethod);
        }
    }

    public virtual void DropOnOwner(NetPacket packet, TransportMethod? transportMethod) { }
    public virtual void DropOnNonOwner(OtherPlayer otherPlayer, NetPacket packet, TransportMethod? transportMethod) { }
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
            DropOnNonOwner((OtherPlayer)interactingPlayer, packet, transportMethod);
        }
    }

    protected override void UpdateOnNonOwner()
    {
        base.UpdateOnNonOwner();
        transform.position = Vector3.Lerp(transform.position, position, lerpSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, lerpSpeed * Time.deltaTime);
        //transform.forward = Vector3.Lerp(transform.forward, Vector3.ProjectOnPlane(forward, Vector3.up), lerpSpeed * Time.deltaTime);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.INTERACTABLE_GRAB:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    ClientManager.Instance.CurrentLobby.GameData.ClientPlayers.TryGetValue(user, out ClientPlayer clientPlayer);
                    Grab(clientPlayer, packet, transportMethod);
                    break;
                }
            case CommandType.INTERACTABLE_INTERACT:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    ClientManager.Instance.CurrentLobby.GameData.ClientPlayers.TryGetValue(user, out ClientPlayer clientPlayer);
                    Interact(clientPlayer, packet, transportMethod);
                    break;
                }
            case CommandType.INTERACTABLE_DROP:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    ClientManager.Instance.CurrentLobby.GameData.ClientPlayers.TryGetValue(user, out ClientPlayer clientPlayer);
                    Drop(clientPlayer, packet, transportMethod);
                    break;
                }
            case CommandType.INTERACTABLE_TRANSFORM:
                {
                    position = packet.ReadVector3();
                    rotation = packet.ReadQuaternion();
                    forward = packet.ReadVector3();

                    if (!firstTransformReceived)
                    {
                        firstTransformReceived = true;
                        transform.position = position;
                        transform.rotation = rotation;
                        //transform.forward = Vector3.ProjectOnPlane(forward, Vector3.up);
                    }
                    break;
                }
        }
    }
}
