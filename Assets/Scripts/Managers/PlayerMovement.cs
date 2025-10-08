using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
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
    private bool previousGroundedState = false;
    private bool previousCrouchingState = false;
    private bool previousWalkingState = false;
    private bool previousSprintingState = false;

    private bool locked = false;
    private bool justGrounded = true;
    private float footstepTimer = 0;

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cc = GetComponent<CharacterController>();
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
        Vector3 crouchPos = Player.Instance.IsCrouching ? new Vector3(0, crouchingHeight, 0) : new Vector3(0, standingHeight, 0);
        cameraParent.localPosition = Vector3.MoveTowards(cameraParent.localPosition, crouchPos, 8f * Time.deltaTime);

        // Gravity
        if (Player.Instance.IsGrounded)
        {
            yVelocity = 0f;
            Player.Instance.Jumped = false;

            if (playerJumpValue > 0)
            {
                Player.Instance.Jumped = true;
                Jump(jumpHeight);
            }

            if (!justGrounded)
            {
                justGrounded = true;
                ClientManager.Instance?.CurrentLobby.SendToServer(PacketBuilder.EventGroundHit(ClientManager.Instance.CurrentUser.PlayerId, new GroundHitArgs() { position = transform.position, rotation = transform.rotation }), TransportMethod.Reliable);
            }
        }
        else
        {
            justGrounded = false;
            yVelocity += gravity * Time.fixedDeltaTime;
        }

        // Movement
        moveDir = ((playerMoveValue.x * rightDir) + (playerMoveValue.y * forwardDir)).normalized;
        xVelocity = (Player.Instance.IsSprinting || sprintJump ? sprintMultiplier : Player.Instance.IsCrouching ? crouchMultiplier : 1f) * moveSpeed * moveDir;
        if (!locked)
        {
            cc.Move(xVelocity * Time.deltaTime);
            cc.Move(Vector3.up * yVelocity * Time.deltaTime);
        }

        // Footstep SFX
        if (moveDir.sqrMagnitude > 0.01f && Player.Instance.IsGrounded)
        {
            if (footstepTimer > (Player.Instance.IsSprinting ? (.55f / sprintMultiplier) : .55f))
            {
                footstepTimer = 0;
                FXManager.Instance.PlaySFX("Footstep", 0.5f, transform.position);
            }
        }
        footstepTimer += Time.deltaTime;

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
        previousGroundedState = Player.Instance.IsGrounded;
        previousCrouchingState = Player.Instance.IsCrouching;
        previousWalkingState = Player.Instance.IsWalking;
        previousSprintingState = Player.Instance.IsSprinting;

        Player.Instance.IsGrounded = Physics.CheckSphere(transform.position, .15f, GameResources.Instance.GroundMask);
        Player.Instance.IsCrouching = playerCrouchValue > 0 && Player.Instance.IsGrounded;
        Player.Instance.IsWalking = playerMoveValue.sqrMagnitude > 0 && Player.Instance.IsGrounded;
        Player.Instance.IsSprinting = playerSprintValue > 0f && Player.Instance.IsWalking && !Player.Instance.IsCrouching && Vector3.Angle(moveDir, forwardDir) < 80;

        if (previousGroundedState != Player.Instance.IsGrounded || previousCrouchingState != Player.Instance.IsCrouching || previousWalkingState != Player.Instance.IsWalking || previousSprintingState != Player.Instance.IsSprinting)
        {
            ClientManager.Instance?.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(Player.Instance, PacketBuilder.PlayerAnim(Player.Instance.IsWalking, Player.Instance.IsSprinting, Player.Instance.IsCrouching, Player.Instance.IsGrounded, Player.Instance.Jumped, Player.Instance.Grabbed)), TransportMethod.Reliable);
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
