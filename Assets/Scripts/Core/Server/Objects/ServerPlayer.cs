using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ServerPlayer : ServerObject
{
    public UserData User { get; set; }

    public Rigidbody RB { get; private set; }

    // Movement Data
    public bool IsGrounded { get; set; }
    public bool IsWalking { get; set; }
    public bool IsSprinting { get; set; }
    public bool IsCrouching { get; set; }
    public bool Jumped { get; set; }
    public bool Grabbed { get; set; }

    // Location
    private Vector3 position;
    private Quaternion rotation;
    private Vector3 forward;

    // Interactable
    public ServerInteractable CurrentInteractable { get; set; }

    public void Init(ushort id, ServerLobby lobby, UserData user, Vector3 position, Quaternion rotation, Vector3 forward)
    {
        base.Init(id, lobby);
        User = user;
        RB = GetComponent<Rigidbody>();
        RB.isKinematic = true;
        this.position = position;
        this.rotation = rotation;
        this.forward = forward;
        lobby.GameData.ServerPlayers.Add(User, this);
        SendToGameClientObject(PacketBuilder.PlayerTransform(this.position, this.rotation, this.forward), TransportMethod.Reliable, User);
    }

    public override void Remove()
    {
        if (CurrentInteractable != null)
        {
            CurrentInteractable.Drop(this, User, null, TransportMethod.Reliable);
        }
        lobby.GameData.ServerPlayers.Remove(User);
        base.Remove();
    }

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.PLAYER_TRANSFORM:
                {
                    position = packet.ReadVector3();
                    rotation = packet.ReadQuaternion();
                    forward = packet.ReadVector3();
                    break;
                }
            case CommandType.PLAYER_ANIM:
                {
                    IsWalking = packet.ReadBool();
                    IsSprinting = packet.ReadBool();
                    IsCrouching = packet.ReadBool();
                    IsGrounded = packet.ReadBool();
                    Jumped = packet.ReadBool();
                    Grabbed = packet.ReadBool();
                    SendToGameClientObject(PacketBuilder.PlayerAnim(IsWalking, IsSprinting, IsCrouching, IsGrounded, Jumped, Grabbed), transportMethod ?? TransportMethod.Reliable, User);
                    break;
                }
            case CommandType.PLAYER_GRAB_REQUEST:
                {
                    ushort interactableId = packet.ReadUShort();
                    lobby.GameData.ServerInteractables.TryGetValue(interactableId, out ServerInteractable interactable);
                    if (interactable != null && interactable.Owner == null && CurrentInteractable == null)
                    {
                        interactable.Grab(this, user, packet, transportMethod);
                    }
                    else
                    {
                        // Failed to grab, send state back to client
                        SendToUserClientObject(user, PacketBuilder.PlayerGrabDeny(), transportMethod ?? TransportMethod.Reliable);
                    }
                    break;
                }
            case CommandType.PLAYER_INTERACT_REQUEST:
                {
                    if (CurrentInteractable != null && CurrentInteractable.Owner == this)
                    {
                        CurrentInteractable.Interact(this, user, packet, transportMethod);
                    }
                    else
                    {
                        // Failed to interact, send state back to client
                        SendToUserClientObject(user, PacketBuilder.PlayerInteractDeny(), transportMethod ?? TransportMethod.Reliable);
                    }
                    break;
                }
            case CommandType.PLAYER_DROP_REQUEST:
                {
                    if (CurrentInteractable != null && CurrentInteractable.Owner == this)
                    {
                        CurrentInteractable.Drop(this, user, packet, transportMethod);
                    }
                    else
                    {
                        // Failed to drop, send state back to client
                        SendToUserClientObject(user, PacketBuilder.PlayerDropDeny(), transportMethod ?? TransportMethod.Reliable);
                    }
                    break;
                }
        }
    }

    public override void Tick()
    {
        RB.MovePosition(position);
        RB.MoveRotation(rotation.normalized);
        SendToGameClientObject(PacketBuilder.PlayerTransform(RB.position, RB.rotation, forward), TransportMethod.Unreliable, User);
    }

    public override void UserJoined(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        SendToUserClientObject(joinedUser, PacketBuilder.PlayerTransform(position, rotation, forward), TransportMethod.Reliable);
        SendToUserClientObject(joinedUser, PacketBuilder.PlayerAnim(IsWalking, IsSprinting, IsCrouching, IsGrounded, Jumped, Grabbed), TransportMethod.Reliable);
    }

    public override void UserLeft(UserData leftUser)
    {
        // Nothing
    }
}