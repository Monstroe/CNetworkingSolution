using System.Net;
using UnityEngine;

public class ClientPlayer : ClientTransform
{
    public UserData User { get; set; }

    public bool IsGrounded
    {
        get { return groundedState; }
        set
        {
            if (groundedState == value) return;
            groundedState = value;
            anim.SetBool("IsGrounded", value);
        }
    }
    public bool IsWalking
    {
        get { return walkingState; }
        set
        {
            if (walkingState == value) return;
            walkingState = value;
            anim.SetBool("IsWalking", value);
        }
    }
    public bool IsSprinting
    {
        get { return sprintingState; }
        set
        {
            if (sprintingState == value) return;
            sprintingState = value;
            anim.SetBool("IsSprinting", value);
        }
    }
    public bool IsCrouching
    {
        get { return crouchingState; }
        set
        {
            if (crouchingState == value) return;
            crouchingState = value;
            anim.SetBool("IsCrouching", value);
        }
    }
    public bool Jumped
    {
        get { return jumpingState; }
        set
        {
            if (jumpingState == value) return;
            jumpingState = value;
            if (value) anim.SetTrigger("Jumped");
        }
    }

    public bool Grabbed
    {
        get { return grabbingState; }
        set
        {
            if (grabbingState == value) return;
            grabbingState = value;
            if (value) anim.SetTrigger("Grabbed");
        }
    }

    public ClientInteractable CurrentInteractable { get; set; } = null;

    // Animations
    private Animator anim;
    private bool groundedState = false;
    private bool crouchingState = false;
    private bool walkingState = false;
    private bool sprintingState = false;
    private bool jumpingState = false;
    private bool grabbingState = false;

    public override void Init(ushort id, ClientLobby lobby)
    {
        base.Init(id, lobby);
        lobby.GetService<PlayerClientService>().ClientPlayers.Add(User, this);
        anim = GetComponentInChildren<Animator>();
    }

    public override void Remove()
    {
        lobby.GetService<PlayerClientService>().ClientPlayers.Remove(User);
        base.Remove();
    }

    protected override void UpdateOnNonOwner()
    {
        //base.UpdateOnNonOwner(); // Disable base interpolation of rotation (using forward only)
        transform.position = Vector3.Lerp(transform.position, receivedPosition, lerpSpeed * Time.deltaTime);
        transform.forward = Vector3.Lerp(transform.forward, Vector3.ProjectOnPlane(receivedForward, Vector3.up), lerpSpeed * Time.deltaTime);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        //base.ReceiveData(packet, serviceType, commandType, transportMethod); // Disabled to prevent double handling of OBJECT_TRANSFORM
        switch (commandType)
        {
            case CommandType.OBJECT_TRANSFORM:
                {
                    receivedPosition = packet.ReadVector3();
                    receivedRotation = packet.ReadQuaternion();
                    receivedForward = packet.ReadVector3();

                    if (!firstTransformReceived)
                    {
                        firstTransformReceived = true;
                        transform.position = receivedPosition;
                        transform.forward = Vector3.ProjectOnPlane(receivedForward, Vector3.up);
                    }
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
                    break;
                }
        }
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }
}
