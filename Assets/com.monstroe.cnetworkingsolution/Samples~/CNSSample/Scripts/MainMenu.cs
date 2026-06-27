using System;
using TMPro;
using UnityEngine;
using Monstroe.CNetworkingSolution;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject multiplayerMenu;
    [SerializeField] private GameObject loadGameMenu;
    [SerializeField] private TMP_InputField lobbyIdInputField;

    private ClientManager clientManager;

    void Start()
    {
        clientManager = FindFirstObjectByType<ClientManager>();
        clientManager.OnConnectionAccepted += ConnectionAccepted;
        clientManager.OnConnectionRejected += ConnectionRejected;
        ResetMenu();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnDestroy()
    {
        if (clientManager != null)
        {
            clientManager.OnConnectionAccepted -= ConnectionAccepted;
            clientManager.OnConnectionRejected -= ConnectionRejected;
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
        ServerManager serverManager = Instantiate(NetResources.Instance.ServerPrefab.gameObject).GetComponent<ServerManager>();
        serverManager.RegisterTransport<LocalTransport>();
        serverManager.StartTransports();
        clientManager.SetConnectionData(new ConnectionData()
        {
            LobbyConnectionType = LobbyConnectionType.JoinOrCreate
        });
        clientManager.RegisterTransport<LocalTransport>();
        clientManager.StartTransport();
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
        ServerManager serverManager = Instantiate(NetResources.Instance.ServerPrefab.gameObject).GetComponent<ServerManager>();
        serverManager.RegisterTransport<LocalTransport>();
        serverManager.RegisterTransport<CNetTransport>();
        serverManager.StartTransports();
        clientManager.SetConnectionData(new ConnectionData()
        {
            LobbyConnectionType = LobbyConnectionType.Create
        });
        clientManager.RegisterTransport<LocalTransport>();
        clientManager.StartTransport();
    }

    public void JoinLobby()
    {
        if (!int.TryParse(lobbyIdInputField.text, out int parsedId))
        {
            return;
        }

        clientManager.SetConnectionData(new ConnectionData()
        {
            LobbyId = parsedId,
            LobbyConnectionType = LobbyConnectionType.JoinIfExists
        });
        clientManager.RegisterTransport<CNetTransport>();
        clientManager.StartTransport();
    }

    public void ResetMenu()
    {
        multiplayerMenu.SetActive(false);
        loadGameMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
}
