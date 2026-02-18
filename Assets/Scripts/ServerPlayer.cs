using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ServerPlayer : ServerEntity
{
    public UserData User { get; set; }

    // Movement Data
    public bool IsGrounded { get; set; }
    public bool IsWalking { get; set; }
    public bool IsSprinting { get; set; }
    public bool IsCrouching { get; set; }
    public bool Jumped { get; set; }
    public bool Grabbed { get; set; }

    // Interactable
    public ServerInteractable CurrentInteractable { get; set; }

    public void Init(ushort id, ServerLobby lobby, UserData user)
    {
        base.Init(id, lobby);
        User = user;
        RB.isKinematic = true;
        lobby.GetService<PlayerServerService>().ServerPlayers.Add(User, this);
    }

    public override void Remove()
    {
        if (CurrentInteractable != null)
        {
            CurrentInteractable.Drop(this, User, null, TransportMethod.Reliable);
        }
        lobby.GetService<PlayerServerService>().ServerPlayers.Remove(User);
        base.Remove();
    }

    [Rpc]
    public void Jump(float height, UserData user, Vector3?[] position = null)
    {
        Debug.Log($"SERVER: Player {Id} Jumped with height {height}, position {position[0]},{position[1]}, user {user.Settings.UserName}");
        InvokeOnGameClientObjects("Jump", height, user, position);
    }

    [EventHandler]
    public void OnPlayerHealthChange(EntityHealthChangeEvent evt)
    {
        if (evt.Entity == this)
        {
            Debug.Log($"SERVER: Player {Id} Health changed by {evt.HealthChange}");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"SERVER: Player {Id} Collided with {collision.gameObject.name}");
    }

    public override void ReceiveData(UserData user, NetPacket packet, CommandType commandType, TransportMethod? transportMethod)
    {
        base.ReceiveData(user, packet, serviceType, commandType, transportMethod);
        switch (commandType)
        {
            case CommandType.PLAYER_ANIM:
                {
                    IsWalking = packet.ReadBool();
                    IsSprinting = packet.ReadBool();
                    IsCrouching = packet.ReadBool();
                    IsGrounded = packet.ReadBool();
                    Jumped = packet.ReadBool();
                    Grabbed = packet.ReadBool();
                    SendToGameClientObjects(PacketBuilder.PlayerAnim(IsWalking, IsSprinting, IsCrouching, IsGrounded, Jumped, Grabbed), transportMethod ?? TransportMethod.Reliable, User);
                    break;
                }
            case CommandType.PLAYER_GRAB_REQUEST:
                {
                    ushort interactableId = packet.ReadUShort();
                    lobby.GetService<InteractableServerService>().ServerInteractables.TryGetValue(interactableId, out ServerInteractable interactable);
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
}