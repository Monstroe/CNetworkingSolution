using UnityEngine;

public class ChatClientService : ClientService
{
    void Start()
    {
        ClientManager.Instance.CurrentLobby.RegisterService(ServiceType.CHAT, this);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.CHAT_MESSAGE:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    string message = packet.ReadString();
                    Chat.Instance.AddChatMessage($"{user.Settings.UserName}: {message}", Color.white);
                    break;
                }
            case CommandType.CHAT_USER_JOINED:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    Chat.Instance.AddUserJoinedMessage(user);
                    break;
                }
            case CommandType.CHAT_USER_LEFT:
                {
                    byte playerId = packet.ReadByte();
                    UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                    Chat.Instance.AddUserLeftMessage(user);
                    break;
                }
        }
    }
}
