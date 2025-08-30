using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ServerPlayer : ServerObject
{
    public UserData User { get; private set; }

    // Movement Data
    public bool IsGrounded { get; set; }
    public bool IsWalking { get; set; }
    public bool IsSprinting { get; set; }
    public bool IsCrouching { get; set; }
    public bool Jumped { get; set; }
    public bool Grabbed { get; set; }

    // Location
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Forward { get; set; }

    // Interactable
    public ServerInteractable CurrentInteractable { get; set; }

    public ServerPlayer(ushort id, UserData user) : base(id)
    {
        this.User = user;
    }

    public override void ReceiveData(ServerLobby lobby, UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.PLAYER_STATE:
                {
                    Position = packet.ReadVector3();
                    Rotation = packet.ReadQuaternion();
                    Forward = packet.ReadVector3();
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
                    lobby.SendToGame(PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerAnim(IsWalking, IsSprinting, IsCrouching, IsGrounded, Jumped, Grabbed)), transportMethod ?? TransportMethod.Reliable, User);
                    break;
                }
        }
    }

    public override void Tick(ServerLobby lobby)
    {
        lobby.SendToGame(PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerState(Position, Rotation, Forward)), TransportMethod.Unreliable, User);
    }
}