using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractableInteract : MonoBehaviour
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

    private IEnumerator NetworkedPlayerGrab(ClientInteractable interactable)
    {
        grabCached = true;
        ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(Player.Instance, PacketBuilder.PlayerGrabRequest(interactable.Id)), TransportMethod.Reliable);
        yield return new WaitUntil(() => Player.Instance.CurrentInteractable != null && Player.Instance.CurrentInteractable.Id == interactable.Id);
        grabCached = false;
    }

    private IEnumerator NetworkedPlayerDrop()
    {
        grabCached = true;
        ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(Player.Instance, PacketBuilder.PlayerDropRequest()), TransportMethod.Reliable);
        yield return new WaitUntil(() => Player.Instance.CurrentInteractable == null);
        grabCached = false;
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
                    StartCoroutine(NetworkedPlayerGrab(interactable));
                }
            }
        }
        else
        {
            Player.Instance.Grabbed = false;
        }

        if (playerDrop.action.WasPressedThisFrame() && Player.Instance.CurrentInteractable != null)
        {
            StartCoroutine(NetworkedPlayerDrop());
        }

        if (playerInteract.action.WasPressedThisFrame() && Player.Instance.CurrentInteractable != null)
        {
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(Player.Instance, PacketBuilder.PlayerInteractRequest()), TransportMethod.Reliable);
        }

        // DEBUG SPAWN ITEMS
        if (Input.GetKeyDown(KeyCode.X))
        {
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.ObjectSpawnRequest("Assets/GameAssets/Client/Items/ClientBasicItem.prefab", transform.position, transform.rotation, false), TransportMethod.Reliable);
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, interactionDistance, GameResources.Instance.InteractionMask))
            {
                if (hit.collider.TryGetComponent(out ClientInteractable interactable))
                {
                    if (interactable.Owner != null && interactable.Owner.Id == Player.Instance.Id)
                    {
                        ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.ObjectDestroyRequest(interactable.Id), TransportMethod.Reliable);
                    }
                }
            }
        }
    }

    void Animate()
    {
        if (previousGrabState != Player.Instance.Grabbed)
        {
            ClientManager.Instance.CurrentLobby.SendToServer(PacketBuilder.ObjectCommunication(Player.Instance, PacketBuilder.PlayerAnim(Player.Instance.IsWalking, Player.Instance.IsSprinting, Player.Instance.IsCrouching, Player.Instance.IsGrounded, Player.Instance.Jumped, Player.Instance.Grabbed)), TransportMethod.Reliable);
        }
    }

    public void SetInteractable(ClientInteractable interactable)
    {
        Player.Instance.CurrentInteractable = interactable;
        grabCached = false;
    }
}
