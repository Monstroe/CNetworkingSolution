using TMPro;
using UnityEngine;

public class Chat : MonoBehaviour
{
    public static Chat Instance { get; private set; }

    public bool IsSelected { get; private set; } = false;

    [SerializeField] private GameObject chatContainer;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private GameObject chatMessagePrefab;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of Chat detected. Destroying duplicate instance.");
            Destroy(gameObject);
            return;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ClientManager.Instance.CurrentLobby.GetService<GameClientService>().OnGameUserJoined += AddUserJoinedMessage;
        ClientManager.Instance.CurrentLobby.GetService<LobbyClientService>().OnLobbyUserLeft += AddUserLeftMessage;
        ClientManager.Instance.CurrentLobby.GetService<ChatClientService>().Chat.OnChatMessageReceived += ReceivedMessage;
    }

    void OnDestroy()
    {
        ClientManager.Instance.CurrentLobby.GetService<GameClientService>().OnGameUserJoined -= AddUserJoinedMessage;
        ClientManager.Instance.CurrentLobby.GetService<LobbyClientService>().OnLobbyUserLeft -= AddUserLeftMessage;
        ClientManager.Instance.CurrentLobby.GetService<ChatClientService>().Chat.OnChatMessageReceived -= ReceivedMessage;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!IsSelected)
            {
                ActivateChat();
            }
            else
            {
                if (inputField.text.Length > 0)
                {
                    ClientManager.Instance.CurrentLobby.GetService<ChatClientService>().Chat.SendChat(inputField.text);
                }
                DeactivateChat();
            }

        }

        if (Input.GetKeyDown(KeyCode.Escape) && IsSelected)
        {
            DeactivateChat();
        }
    }

    public void ActivateChat()
    {
        inputField.ActivateInputField();
        IsSelected = true;
        Player.Instance.ControlsEnabled = false;
    }

    public void DeactivateChat()
    {
        inputField.DeactivateInputField();
        inputField.text = "";
        IsSelected = false;
        Player.Instance.ControlsEnabled = true;
    }

    private void AddUserJoinedMessage(UserData user)
    {
        string message = $"{user.Settings.UserName} has joined the game.";
        AddChatMessage(message, Color.green);
    }

    private void AddUserLeftMessage(UserData user)
    {
        string message = $"{user.Settings.UserName} has left the game.";
        AddChatMessage(message, Color.red);
    }

    private void ReceivedMessage(UserData user, string message)
    {
        AddChatMessage($"{user.Settings.UserName}: {message}", Color.white);
    }

    public void AddChatMessage(string message, Color color)
    {
        GameObject chatMessage = Instantiate(chatMessagePrefab, chatContainer.transform);
        chatMessage.transform.SetAsFirstSibling();
        chatMessage.GetComponent<TMP_Text>().text = message;
        chatMessage.GetComponent<TMP_Text>().color = color;
    }
}
