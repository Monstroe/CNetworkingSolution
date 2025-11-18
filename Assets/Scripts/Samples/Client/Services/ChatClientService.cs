using System.Net;
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
                    string userName = packet.ReadString();
                    Chat.Instance.AddUserJoinedMessage(userName);
                    break;
                }
            case CommandType.CHAT_USER_LEFT:
                {
                    string userName = packet.ReadString();
                    Chat.Instance.AddUserLeftMessage(userName);
                    break;
                }
        }
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }
}
