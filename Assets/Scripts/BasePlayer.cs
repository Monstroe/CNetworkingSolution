using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class BasePlayer : ClientPlayer
{
    public static BasePlayer Instance { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of BasePlayer detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    public override void Init(ushort id, ClientLobby lobby)
    {
        base.Init(id, lobby);
        Initialized = true;
        Debug.Log("BasePlayer initialized.");
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
            Debug.Log("BasePlayer controls enabled.");
            ControlsEnabled = true;
        };
    }

    protected override void UpdateOnNonOwner()
    {
        // Prevent lerp from running in ClientPlayer since BasePlayer handles own transform
    }
}
