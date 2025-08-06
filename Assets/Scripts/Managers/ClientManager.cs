using UnityEngine;

public class ClientManager : MonoBehaviour
{
    public static ClientManager Instance { get; private set; }

    public UserData UserData { get; private set; } = new UserData();

    private NetTransport transport;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("<color=yellow><b>CNS</b></color>: Multiple instances of ClientManager detected. Destroying duplicate instance.");
            Destroy(gameObject);
        }
    }

    public void Init(NetTransport transport)
    {
        transport.Initialize();

        ClientLobby.Instance.Init(-1, transport);
    }

    public void Disconnect()
    {
        transport.Disconnect();
    }

    public void Shutdown()
    {
        transport.Shutdown();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
