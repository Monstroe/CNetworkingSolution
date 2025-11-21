using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class Player : ClientPlayer
{
    public static Player Instance { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of Player detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    public bool MovementEnabled { get; private set; } = false;
    public bool InteractEnabled { get; private set; } = false;

    public PlayerMovement PlayerMovement { get { return playerMovement; } }
    public InteractableInteract InteractableInteract { get { return playerInteract; } }

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private InteractableInteract playerInteract;

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
        GetComponent<PlayerInput>().actions.Enable();
        GameManager.Instance.OnGameInitialized += () =>
        {
            SetMovementActive(true);
            SetInteractActive(true);
        };
    }

    public void SetMovementActive(bool value)
    {
        MovementEnabled = value;
    }

    public void SetInteractActive(bool value)
    {
        InteractEnabled = value;
    }

    protected override void UpdateOnNonOwner()
    {
        // Prevent lerp from running in ClientPlayer since Player handles own transform
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.PLAYER_TRANSFORM:
                {
                    Vector3 position = packet.ReadVector3();
                    Quaternion rotation = packet.ReadQuaternion();
                    Vector3 forward = packet.ReadVector3();
                    PlayerMovement.SetTransform(position, rotation, forward);
                    break;
                }
        }
    }
}
