using TMPro;
using UnityEngine;

public class ChatMenu : MonoBehaviour
{
    public delegate void ChatSelectedHandler(bool isSelected);
    public event ChatSelectedHandler OnChatSelected;

    public static ChatMenu Instance { get; private set; }

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
            Debug.LogWarning("Multiple instances of ChatMenu detected. Destroying duplicate instance.");
            Destroy(gameObject);
            return;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ClientManager.Instance.CurrentLobby.GetService<ChatClientService>().OnChatUserJoined += AddUserJoinedMessage;
        ClientManager.Instance.CurrentLobby.GetService<ChatClientService>().OnChatUserLeft += AddUserLeftMessage;
        ClientManager.Instance.CurrentLobby.GetService<ChatClientService>().OnChatMessageReceived += ReceivedMessage;
    }

    void OnDestroy()
    {
        ClientManager.Instance.CurrentLobby.GetService<ChatClientService>().OnChatUserJoined -= AddUserJoinedMessage;
        ClientManager.Instance.CurrentLobby.GetService<ChatClientService>().OnChatUserLeft -= AddUserLeftMessage;
        ClientManager.Instance.CurrentLobby.GetService<ChatClientService>().OnChatMessageReceived -= ReceivedMessage;
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
                    ClientManager.Instance.CurrentLobby.GetService<ChatClientService>().SendChat(inputField.text);
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
        OnChatSelected?.Invoke(IsSelected);
    }

    public void DeactivateChat()
    {
        inputField.DeactivateInputField();
        inputField.text = "";
        IsSelected = false;
        OnChatSelected?.Invoke(IsSelected);
    }

    private void AddUserJoinedMessage(UserData joinedUser, string welcomeMessage)
    {
        AddChatMessage(welcomeMessage, Color.green);
    }

    private void AddUserLeftMessage(UserData leftUser, string farewellMessage)
    {
        AddChatMessage(farewellMessage, Color.red);
    }

    private void ReceivedMessage(UserData user, string message)
    {
        AddChatMessage($"Player {user.PlayerId}: {message}", Color.white);
    }

    public void AddChatMessage(string message, Color color)
    {
        GameObject chatMessage = Instantiate(chatMessagePrefab, chatContainer.transform);
        chatMessage.transform.SetAsFirstSibling();
        chatMessage.GetComponent<TMP_Text>().text = message;
        chatMessage.GetComponent<TMP_Text>().color = color;
    }
}
