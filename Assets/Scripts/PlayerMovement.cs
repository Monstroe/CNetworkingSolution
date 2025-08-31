using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public bool IsGrounded
    {
        get { return groundedState; }
        set
        {
            if (groundedState == value) return;
            groundedState = value;
            sprintJump = !value && IsSprinting;
            updateAnimationState = true;
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
            updateAnimationState = true;
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
            updateAnimationState = true;
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
            updateAnimationState = true;
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
            updateAnimationState = true;
            if (value) anim.SetTrigger("Jumped");
        }
    }

    [Header("Player Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float gravity;
    [SerializeField] private float sprintMultiplier;
    [SerializeField] private float crouchMultiplier;
    [SerializeField] private float crouchLower;
    [SerializeField] private Transform cameraParent;

    [Space]
    [Header("Player Rotation")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity;

    [Header("Player Movement Controls")]
    [SerializeField] private InputActionProperty playerMove;
    [SerializeField] private InputActionProperty playerLook;
    [SerializeField] private InputActionProperty playerJump;
    [SerializeField] private InputActionProperty playerSprint;
    [SerializeField] private InputActionProperty playerCrouch;

    private Vector2 playerMoveValue;
    private Vector2 playerLookValue;
    private float playerJumpValue;
    private float playerSprintValue;
    private float playerCrouchValue;

    // Movement
    private CharacterController cc;
    private Vector3 moveDir, forwardDir, rightDir;
    private float xRotation, yRotation;
    private Vector3 xVelocity;
    private float yVelocity;

    // Jumping
    private bool sprintJump;

    // Crouching
    private float standingHeight;
    private float crouchingHeight;

    // Animations
    private Animator anim;
    private bool groundedState = false;
    private bool crouchingState = false;
    private bool walkingState = false;
    private bool sprintingState = false;
    private bool jumpingState = false;
    private bool updateAnimationState = false;

    private bool locked = false;

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        standingHeight = cameraParent.localPosition.y;
        crouchingHeight = cameraParent.localPosition.y - crouchLower;
    }

    // Update is called once per frame
    void Update()
    {
        if (Player.Instance.MovementEnabled)
        {
            Input();
            Rotate();
            Animate();
        }
    }

    void FixedUpdate()
    {
        if (Player.Instance.MovementEnabled)
        {
            Move();

            if (locked)
            {
                locked = false;
            }
        }
    }

    void Input()
    {
        playerMoveValue = playerMove.action.ReadValue<Vector2>();
        playerLookValue = playerLook.action.ReadValue<Vector2>();
        playerJumpValue = playerJump.action.ReadValue<float>();
        playerSprintValue = playerSprint.action.ReadValue<float>();
        playerCrouchValue = playerCrouch.action.ReadValue<float>();
    }

    void Move()
    {
        // Directions
        forwardDir = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        rightDir = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        // Crouching
        Vector3 crouchPos = IsCrouching ? new Vector3(0, crouchingHeight, 0) : new Vector3(0, standingHeight, 0);
        cameraParent.localPosition = Vector3.MoveTowards(cameraParent.localPosition, crouchPos, 8f * Time.deltaTime);

        // Gravity
        if (IsGrounded)
        {
            yVelocity = 0f;
            Jumped = false;

            if (playerJumpValue > 0)
            {
                Jumped = true;
                Jump(jumpHeight);
            }
        }
        else
        {
            yVelocity += gravity * Time.fixedDeltaTime;
        }

        // Movement
        moveDir = ((playerMoveValue.x * rightDir) + (playerMoveValue.y * forwardDir)).normalized;
        xVelocity = (IsSprinting || sprintJump ? sprintMultiplier : IsCrouching ? crouchMultiplier : 1f) * moveSpeed * moveDir;
        if (!locked)
        {
            cc.Move(xVelocity * Time.deltaTime);
            cc.Move(Vector3.up * yVelocity * Time.deltaTime);
        }

        // Networking
        ClientManager.Instance?.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(Player.Instance, PacketBuilder.PlayerTransform(transform.position, transform.rotation, cameraTransform.forward)), TransportMethod.Unreliable);
    }

    void Rotate()
    {
        xRotation += -playerLookValue.y * mouseSensitivity;
        yRotation += playerLookValue.x * mouseSensitivity;

        xRotation = Mathf.Clamp(xRotation, -89, 89);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    void Animate()
    {
        IsGrounded = Physics.CheckSphere(transform.position, .15f, GameResources.Instance.GroundMask);
        IsCrouching = playerCrouchValue > 0 && IsGrounded;
        IsWalking = playerMoveValue.sqrMagnitude > 0 && IsGrounded;
        IsSprinting = playerSprintValue > 0f && IsWalking && !IsCrouching && Vector3.Angle(moveDir, forwardDir) < 80;

        if (updateAnimationState)
        {
            ClientManager.Instance?.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(Player.Instance, PacketBuilder.PlayerAnim(IsWalking, IsSprinting, IsCrouching, IsGrounded, Jumped, Player.Instance.PlayerInteract.Grabbed)), TransportMethod.Reliable);
            updateAnimationState = false;
        }
    }

    public void Jump(float height)
    {
        yVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * height);
    }

    public void SetTransform(Vector3 position, Quaternion rotation, Vector3 forward)
    {
        locked = true;
        transform.position = position;
        xRotation = rotation.eulerAngles.x;
        xRotation = Mathf.Clamp(xRotation, -89, 89);
        yRotation = rotation.eulerAngles.y;
    }
}
