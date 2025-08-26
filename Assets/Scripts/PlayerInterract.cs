using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    public bool Grabbed
    {
        get { return grabbingState; }
        set
        {
            if (grabbingState == value) return;
            grabbingState = value;
            updateAnimationState = true;
            if (value) anim.SetTrigger("Grabbed");
        }
    }

    public ClientInteractable CurrentInteractable { get; private set; }

    [Header("Player Interaction")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private Transform cameraTransform;

    [Header("Player Interact Controls")]
    [SerializeField] private InputActionProperty playerGrab;
    [SerializeField] private InputActionProperty playerDrop;
    [SerializeField] private InputActionProperty playerInteract;

    private bool grabCached = false;

    // Animations
    private Animator anim;
    private bool grabbingState = false;
    private bool updateAnimationState = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Player.Instance.InteractEnabled)
        {
            Interact();
            Animate();
        }
    }

    void Interact()
    {
        if (grabCached)
        {
            return;
        }

        if (playerGrab.action.WasPressedThisFrame() && CurrentInteractable == null)
        {
            Grabbed = true;
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, interactionDistance, GameResources.Instance.InteractionMask))
            {
                if (hit.collider.TryGetComponent(out ClientInteractable interactable))
                {
                    grabCached = true;
                    ClientManager.Instance?.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(interactable, PacketBuilder.PlayerGrab((byte)Player.Instance.Id)), TransportMethod.Reliable);
                }
            }
        }
        else
        {
            Grabbed = false;
        }

        if (playerDrop.action.WasPressedThisFrame() && CurrentInteractable != null)
        {
            ClientManager.Instance?.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(CurrentInteractable, PacketBuilder.PlayerDrop((byte)Player.Instance.Id)), TransportMethod.Reliable);
        }

        if (playerInteract.action.WasPressedThisFrame() && CurrentInteractable != null)
        {
            ClientManager.Instance?.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(CurrentInteractable, PacketBuilder.PlayerInteract((byte)Player.Instance.Id)), TransportMethod.Reliable);
        }
    }

    void Animate()
    {
        if (updateAnimationState)
        {
            ClientManager.Instance?.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(Player.Instance, PacketBuilder.PlayerAnim(Player.Instance.PlayerMovement.IsWalking, Player.Instance.PlayerMovement.IsSprinting, Player.Instance.PlayerMovement.IsCrouching, Player.Instance.PlayerMovement.IsGrounded, Player.Instance.PlayerMovement.Jumped, Grabbed)), TransportMethod.Reliable);
            updateAnimationState = false;
        }
    }

    public void SetInteractable(ClientInteractable interactable)
    {
        CurrentInteractable = interactable;
        grabCached = false;
    }

    public void ResetInteractable()
    {
        CurrentInteractable = null;
    }
}
