using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class Player : ClientPlayer
{
    public static Player Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of Player detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    public bool MovementEnabled { get; private set; } = false;
    public bool InteractEnabled { get; private set; } = false;

    public PlayerMovement PlayerMovement { get { return playerMovement; } }
    public PlayerInteract PlayerInteract { get { return playerInteract; } }

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInteract playerInteract;

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

    // Update is called once per frame
    void Update()
    {

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
