using System;
using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject multiplayerMenu;
    [SerializeField] private GameObject loadGameMenu;
    [SerializeField] private TMP_InputField lobbyIdInputField;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of MainMenu detected. Destroying duplicate instance.");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        ClientManager.Instance.OnConnectionAccepted += ConnectionAccepted;
        ClientManager.Instance.OnConnectionRejected += ConnectionRejected;
        ResetMenu();
    }

    private void ConnectionAccepted(ConnectionAcceptedArgs args)
    {
        mainMenu.SetActive(false);
        multiplayerMenu.SetActive(false);
        loadGameMenu.SetActive(true);
    }

    private void ConnectionRejected(ConnectionRejectedArgs args)
    {
        ResetMenu();
    }

    public void StartSinglePlayer()
    {
        Instantiate(NetResources.Instance.ServerPrefab.gameObject).GetComponent<ServerManager>();
        ServerManager.Instance.RegisterTransport<LocalTransport>();
        ServerManager.Instance.StartTransports();
        ClientManager.Instance.SetConnectionData(new ConnectionData()
        {
            LobbyConnectionType = LobbyConnectionType.JoinOrCreate
        });
        ClientManager.Instance.RegisterTransport<LocalTransport>();
        ClientManager.Instance.StartTransport();
    }

    public void StartMultiPlayer()
    {
        ToMultiplayerMenu();
    }

    public void ToMultiplayerMenu()
    {
        mainMenu.SetActive(false);
        loadGameMenu.SetActive(false);
        multiplayerMenu.SetActive(true);
    }

    public void CreateLobby()
    {
        Instantiate(NetResources.Instance.ServerPrefab.gameObject).GetComponent<ServerManager>();
        ServerManager.Instance.RegisterTransport<LocalTransport>();
        ServerManager.Instance.RegisterTransport<CNetTransport>();
        ServerManager.Instance.StartTransports();
        ClientManager.Instance.SetConnectionData(new ConnectionData()
        {
            LobbyConnectionType = LobbyConnectionType.Create
        });
        ClientManager.Instance.RegisterTransport<LocalTransport>();
        ClientManager.Instance.StartTransport();
    }

    public void JoinLobby()
    {
        if (!int.TryParse(lobbyIdInputField.text, out int parsedId))
        {
            return;
        }

        ClientManager.Instance.SetConnectionData(new ConnectionData()
        {
            LobbyId = parsedId,
            LobbyConnectionType = LobbyConnectionType.JoinIfExists
        });
        ClientManager.Instance.RegisterTransport<CNetTransport>();
        ClientManager.Instance.StartTransport();
    }

    public void ResetMenu()
    {
        multiplayerMenu.SetActive(false);
        loadGameMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
}
