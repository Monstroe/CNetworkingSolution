using System;
using UnityEngine;

public class ServerPlayer : ServerTransform
{
    public UserData User { get; set; }

    // Movement Data
    public bool IsGrounded { get; set; }
    public bool IsWalking { get; set; }
    public bool IsSprinting { get; set; }
    public bool IsCrouching { get; set; }
    public bool Jumped { get; set; }
    public bool Grabbed { get; set; }

    public override void Init(ushort id, ServerLobby lobby)
    {
        base.Init(id, lobby);
        RB.isKinematic = true;
        lobby.GetService<PlayerServerService>().ServerPlayers.Add(Owner, this);
    }

    public override void Remove()
    {
        base.Remove();
        lobby.GetService<PlayerServerService>().ServerPlayers.Remove(Owner);
    }

    [Rpc]
    private void SyncAnimRpc(byte ownerId, bool isWalking, bool isSprinting, bool isCrouching, bool isGrounded, bool jumped, bool grabbed)
    {
        IsWalking = isWalking;
        IsSprinting = isSprinting;
        IsCrouching = isCrouching;
        IsGrounded = isGrounded;
        Jumped = jumped;
        Grabbed = grabbed;

        InvokeOnGameClientObjects(nameof(SyncAnimRpc), ownerId, isWalking, isSprinting, isCrouching, isGrounded, jumped, grabbed);
    }
}