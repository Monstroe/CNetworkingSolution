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
        }
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
                    SubmitMessage(inputField.text);
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
        Player.Instance.SetMovementActive(false);
        Player.Instance.SetInteractActive(false);
    }

    public void DeactivateChat()
    {
        inputField.DeactivateInputField();
        inputField.text = "";
        IsSelected = false;
        Player.Instance.SetMovementActive(true);
        Player.Instance.SetInteractActive(true);
    }

    public void SubmitMessage(string message)
    {
        ClientManager.Instance?.CurrentLobby.SendToServer(PacketBuilder.ChatMessage(ClientManager.Instance?.CurrentUser, message), TransportMethod.Reliable);
    }

    public void AddUserJoinedMessage(UserData user)
    {
        string message = $"{user.Settings.UserName} has joined the game.";
        AddChatMessage(message, Color.green);
    }

    public void AddUserLeftMessage(UserData user)
    {
        Debug.Log($"User {user.UserId} ({user.PlayerId}) left the lobby.");
        string message = $"{user.Settings.UserName} has left the game.";
        AddChatMessage(message, Color.red);
    }

    public void AddChatMessage(string message, Color color)
    {
        GameObject chatMessage = Instantiate(chatMessagePrefab, chatContainer.transform);
        chatMessage.transform.SetAsFirstSibling();
        chatMessage.GetComponent<TMP_Text>().text = message;
        chatMessage.GetComponent<TMP_Text>().color = color;
    }
}
