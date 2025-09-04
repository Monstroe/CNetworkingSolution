using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [Header("Player Interaction")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private Transform cameraTransform;

    [Header("Player Interact Controls")]
    [SerializeField] private InputActionProperty playerGrab;
    [SerializeField] private InputActionProperty playerDrop;
    [SerializeField] private InputActionProperty playerInteract;

    private bool grabCached = false;

    // Animations
    private bool previousGrabState = false;

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

        previousGrabState = Player.Instance.Grabbed;

        if (playerGrab.action.WasPressedThisFrame() && Player.Instance.CurrentInteractable == null)
        {
            Player.Instance.Grabbed = true;
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
            Player.Instance.Grabbed = false;
        }

        if (playerDrop.action.WasPressedThisFrame() && Player.Instance.CurrentInteractable != null)
        {
            ClientManager.Instance?.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(Player.Instance.CurrentInteractable, PacketBuilder.PlayerDrop((byte)Player.Instance.Id)), TransportMethod.Reliable);
        }

        if (playerInteract.action.WasPressedThisFrame() && Player.Instance.CurrentInteractable != null)
        {
            ClientManager.Instance?.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(Player.Instance.CurrentInteractable, PacketBuilder.PlayerInteract((byte)Player.Instance.Id)), TransportMethod.Reliable);
        }
    }

    void Animate()
    {
        if (previousGrabState != Player.Instance.Grabbed)
        {
            ClientManager.Instance?.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(Player.Instance, PacketBuilder.PlayerAnim(Player.Instance.IsWalking, Player.Instance.IsSprinting, Player.Instance.IsCrouching, Player.Instance.IsGrounded, Player.Instance.Jumped, Player.Instance.Grabbed)), TransportMethod.Reliable);
        }
    }

    public void SetInteractable(ClientInteractable interactable)
    {
        Player.Instance.CurrentInteractable = interactable;
        grabCached = false;
    }
}
