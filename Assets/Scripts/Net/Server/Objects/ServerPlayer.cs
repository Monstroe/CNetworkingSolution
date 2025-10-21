using System;
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

    public ServerPlayer(ushort id, ServerLobby lobby, UserData user) : base(id, lobby)
    {
        this.User = user;
    }

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.PLAYER_TRANSFORM:
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

    public override void Tick()
    {
        lobby.SendToGame(PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerTransform(Position, Rotation, Forward)), TransportMethod.Unreliable, User);
    }

    public override void UserJoined(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        lobby.SendToUser(joinedUser, PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerTransform(Position, Rotation, Forward)), TransportMethod.Reliable);
        lobby.SendToUser(joinedUser, PacketBuilder.ObjectCommunication(this, PacketBuilder.PlayerAnim(IsWalking, IsSprinting, IsCrouching, IsGrounded, Jumped, Grabbed)), TransportMethod.Reliable);
        if (CurrentInteractable != null)
        {
            lobby.SendToUser(joinedUser, PacketBuilder.ObjectCommunication(CurrentInteractable, PacketBuilder.PlayerGrab(User.PlayerId)), TransportMethod.Reliable);
        }
    }

    public override void UserLeft(UserData leftUser)
    {
        // Nothing
    }
}