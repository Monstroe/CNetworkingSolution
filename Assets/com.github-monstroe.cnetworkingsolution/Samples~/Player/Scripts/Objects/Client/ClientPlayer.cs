using UnityEngine;
using UnityEngine.InputSystem;
using CNetworkingSolution;

[RequireComponent(typeof(CharacterController))]
public class ClientPlayer : ClientTransform
{
    public static ClientPlayer LocalPlayer { get; private set; }

    public bool ControlsEnabled { get; set; } = false;

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
    private PlayerInput playerInput;
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

    private bool previousGroundedState = false;
    private bool previousCrouchingState = false;
    private bool previousWalkingState = false;
    private bool previousSprintingState = false;

    private bool groundedState = false;
    private bool crouchingState = false;
    private bool walkingState = false;
    private bool sprintingState = false;
    private bool jumpingState = false;
    private bool grabbingState = false;

    // Misc
    private SkinnedMeshRenderer[] meshRenderers;
    private bool locked = false;
    private bool justGrounded = true;
    //private float footstepTimer = 0;

    public override void Init(ushort id, ClientLobby lobby)
    {
        base.Init(id, lobby);
        lobby.GetService<PlayerClientService>().ClientPlayers.Add(Owner, this);

        anim = GetComponentInChildren<Animator>();
        playerInput = GetComponent<PlayerInput>();
        cc = GetComponent<CharacterController>();
        meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        standingHeight = cameraParent.localPosition.y;
        crouchingHeight = cameraParent.localPosition.y - crouchLower;
    }

    public override void Remove()
    {
        base.Remove();
        lobby.GetService<PlayerClientService>().ClientPlayers.Remove(Owner);
    }

    [ClientRpc]
    private void SyncAnimRpc(bool isWalking, bool isSprinting, bool isCrouching, bool isGrounded, bool jumped, bool grabbed)
    {
        IsWalking = isWalking;
        IsSprinting = isSprinting;
        IsCrouching = isCrouching;
        IsGrounded = isGrounded;
        Jumped = jumped;
        Grabbed = grabbed;
    }

    protected override void InitTransform(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rot.eulerAngles.y, 0f));
    }

    protected override void ReceiveTransform(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(Vector3.Lerp(transform.position, pos, lerpSpeed * Time.deltaTime), Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, rot.eulerAngles.y, 0f), lerpSpeed * Time.deltaTime));
    }

    protected override void StartOnOwner()
    {
        base.StartOnOwner();
        ControlsEnabled = true;
        playerInput.enabled = true;
        cameraTransform.gameObject.SetActive(true);

        foreach (var mr in meshRenderers)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
        LocalPlayer = this;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    protected override void StartOnNonOwner()
    {
        base.StartOnNonOwner();
        ControlsEnabled = false;
        playerInput.enabled = false;
        cameraTransform.gameObject.SetActive(false);

        foreach (var mr in meshRenderers)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    protected override void UpdateOnOwner()
    {
        base.UpdateOnOwner();
        if (ControlsEnabled)
        {
            Input();
            Rotate();
            Animate();
        }
        else
        {
            playerMoveValue = Vector2.zero;
            playerLookValue = Vector2.zero;
            playerJumpValue = 0f;
            playerSprintValue = 0f;
            playerCrouchValue = 0f;
        }
    }

    protected override void FixedUpdateOnOwner()
    {
        base.FixedUpdateOnOwner();
        Move();

        if (locked)
        {
            locked = false;
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

            if (!justGrounded)
            {
                justGrounded = true;
            }
        }
        else
        {
            justGrounded = false;
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

        // Footstep SFX
        /*if (moveDir.sqrMagnitude > 0.01f && IsGrounded)
        {
            if (footstepTimer > (IsSprinting ? (.55f / sprintMultiplier) : .55f))
            {
                footstepTimer = 0;
                ClientManager.Instance.CurrentLobby.GetService<FXClientService>().PlaySFX("Footstep.wav", 0.5f, transform.position);
            }
        }
        footstepTimer += Time.deltaTime;*/
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
        previousGroundedState = IsGrounded;
        previousCrouchingState = IsCrouching;
        previousWalkingState = IsWalking;
        previousSprintingState = IsSprinting;

        IsGrounded = Physics.CheckSphere(transform.position, .15f);
        IsCrouching = playerCrouchValue > 0 && IsGrounded;
        IsWalking = playerMoveValue.sqrMagnitude > 0 && IsGrounded;
        IsSprinting = playerSprintValue > 0f && IsWalking && !IsCrouching && Vector3.Angle(moveDir, forwardDir) < 80;

        if (previousGroundedState != IsGrounded || previousCrouchingState != IsCrouching || previousWalkingState != IsWalking || previousSprintingState != IsSprinting)
        {
            InvokeOnServerObject(nameof(SyncAnimRpc), IsWalking, IsSprinting, IsCrouching, IsGrounded, Jumped, Grabbed);
        }
    }

    public void Jump(float height)
    {
        yVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * height);
    }
}
