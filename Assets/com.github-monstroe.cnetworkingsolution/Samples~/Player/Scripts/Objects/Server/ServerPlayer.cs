using System;
using UnityEngine;
using CNetworkingSolution;

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

    [ServerRpc]
    private void SyncAnimRpc(bool isWalking, bool isSprinting, bool isCrouching, bool isGrounded, bool jumped, bool grabbed)
    {
        IsWalking = isWalking;
        IsSprinting = isSprinting;
        IsCrouching = isCrouching;
        IsGrounded = isGrounded;
        Jumped = jumped;
        Grabbed = grabbed;

        InvokeOnGameClientObjects(nameof(SyncAnimRpc), exception: Owner, isWalking, isSprinting, isCrouching, isGrounded, jumped, grabbed);
    }
}