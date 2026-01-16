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

    public override void Init(ushort id, ClientLobby lobby)
    {
        base.Init(id, lobby);
        Initialized = true;
        Debug.Log("Player initialized.");
    }

    public bool Initialized { get; set; } = false;
    public bool ControlsEnabled { get; set; } = false;

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
        ClientManager.Instance.CurrentLobby.GetService<GameClientService>().OnGameInitialized += () =>
        {
            Debug.Log("Player controls enabled.");
            ControlsEnabled = true;
        };
    }

    protected override void UpdateOnNonOwner()
    {
        // Prevent lerp from running in ClientPlayer since Player handles own transform
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Disabled to prevent double handling of OBJECT_TRANSFORM
    }
}
