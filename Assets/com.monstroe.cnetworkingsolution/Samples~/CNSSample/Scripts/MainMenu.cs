using TMPro;
using UnityEngine;
using Monstroe.CNetworkingSolution;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject multiplayerMenu;
    [SerializeField] private GameObject loadGameMenu;
    [SerializeField] private TMP_InputField lobbyIdInputField;

    void Start()
    {
        NetworkManager.Instance.Client.OnConnectionAccepted += ConnectionAccepted;
        NetworkManager.Instance.Client.OnConnectionRejected += ConnectionRejected;
        ResetMenu();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance.Client != null)
        {
            NetworkManager.Instance.Client.OnConnectionAccepted -= ConnectionAccepted;
            NetworkManager.Instance.Client.OnConnectionRejected -= ConnectionRejected;
        }
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
        NetworkManager.Instance.SpawnServer();
        NetworkManager.Instance.Server.RegisterTransport<LocalTransport>();
        NetworkManager.Instance.Server.StartTransports();
        NetworkManager.Instance.Client.SetConnectionData(new ConnectionData()
        {
            LobbyConnectionType = LobbyConnectionType.JoinOrCreate
        });
        NetworkManager.Instance.Client.RegisterTransport<LocalTransport>();
        NetworkManager.Instance.Client.StartTransport();
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
        NetworkManager.Instance.SpawnServer();
        NetworkManager.Instance.Server.RegisterTransport<LocalTransport>();
        NetworkManager.Instance.Server.RegisterTransport<CNetTransport>();
        NetworkManager.Instance.Server.StartTransports();
        NetworkManager.Instance.Client.SetConnectionData(new ConnectionData()
        {
            LobbyConnectionType = LobbyConnectionType.Create
        });
        NetworkManager.Instance.Client.RegisterTransport<LocalTransport>();
        NetworkManager.Instance.Client.StartTransport();
    }

    public void JoinLobby()
    {
        if (!int.TryParse(lobbyIdInputField.text, out int parsedId))
        {
            return;
        }

        NetworkManager.Instance.Client.SetConnectionData(new ConnectionData()
        {
            LobbyId = parsedId,
            LobbyConnectionType = LobbyConnectionType.JoinIfExists
        });
        NetworkManager.Instance.Client.RegisterTransport<CNetTransport>();
        NetworkManager.Instance.Client.StartTransport();
    }

    public void ResetMenu()
    {
        multiplayerMenu.SetActive(false);
        loadGameMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
}
