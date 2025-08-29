using UnityEngine;

public class OtherPlayer : ClientObject
{
    public UserData OtherUser { get; private set; }
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
            Debug.Log($"OtherPlayer {OtherUser.PlayerId} IsWalking set to {value}");
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

    [SerializeField] private float lerpSpeed = 15;

    // Movement
    private Vector3 position;
    private Quaternion rotation;
    private Vector3 forward;

    // Animations
    private Animator anim;
    private bool groundedState = false;
    private bool crouchingState = false;
    private bool walkingState = false;
    private bool sprintingState = false;
    private bool jumpingState = false;
    private bool grabbingState = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, position, lerpSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(Vector3.ProjectOnPlane(rotation.eulerAngles, Vector3.up)), lerpSpeed * Time.deltaTime);
        //transform.forward = Vector3.Lerp(transform.forward, forward, lerpSpeed * Time.deltaTime);
    }

    public void Register(UserData otherUser, Vector3? initialPosition, Quaternion? initialRotation, Vector3? initialForward)
    {
        OtherUser = otherUser;
        position = initialPosition ?? transform.position;
        rotation = initialRotation ?? transform.rotation;
        forward = initialForward ?? transform.forward;
        transform.position = position;
        transform.rotation = rotation;
        transform.forward = forward;
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.PLAYER_STATE:
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
                    break;
                }
            case CommandType.PLAYER_DESTROY:
                {
                    ClientManager.Instance.CurrentLobby.GameData.OtherPlayers.Remove(OtherUser);
                    ClientManager.Instance.CurrentLobby.GameData.ClientObjects.Remove(Id);
                    Destroy(gameObject);
                    break;
                }
        }
    }
}
