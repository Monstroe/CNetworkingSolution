using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 3f;

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject multiplayerMenu;
    [SerializeField] private GameObject singleHostMenu;
    [SerializeField] private TMP_InputField lobbyIdInputField;

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartSinglePlayer()
    {
        ClientManager.Instance.NetMode = NetMode.Local;
        ClientManager.Instance.CreateNewUser();
        ClientManager.Instance.CreateNewLobby();
    }

    public void StartMultiPlayer()
    {
        ClientManager.Instance.CreateNewUser();
#if CNS_SERVER_MULTIPLE || CNS_LOBBY_MULTIPLE
        ToMultiplayerMenu();
#elif CNS_SERVER_SINGLE && CNS_LOBBY_SINGLE && CNS_SYNC_HOST
        ToSingleHostMenu();
#elif CNS_SERVER_SINGLE && CNS_LOBBY_SINGLE
        ClientManager.Instance.JoinExistingLobby(NetResources.Instance.DefaultLobbyId);
#endif
    }

    public void ToMultiplayerMenu()
    {
        mainMenu.SetActive(false);
        multiplayerMenu.SetActive(true);
    }

    public void ToSingleHostMenu()
    {
        mainMenu.SetActive(false);
        singleHostMenu.SetActive(true);
    }

    public void BackToMainMenu()
    {
        multiplayerMenu.SetActive(false);
        singleHostMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void CreateLobby()
    {
        ClientManager.Instance.CreateNewLobby();
    }

    public void JoinLobby()
    {
#if CNS_SERVER_SINGLE && CNS_LOBBY_SINGLE && CNS_SYNC_HOST
        ClientManager.Instance.JoinExistingLobby(NetResources.Instance.DefaultLobbyId);
#else
        if (!int.TryParse(lobbyIdInputField.text, out int parsedId))
        {
            return;
        }

        ClientManager.Instance.JoinExistingLobby(parsedId);
#endif
    }
}
