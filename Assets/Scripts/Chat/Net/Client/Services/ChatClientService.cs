using System.Net;
using UnityEngine;

public class ChatClientService : ClientService
{
    public ClientChat Chat { get; private set; }

    public void SetChat(ClientChat chat)
    {
        Chat = chat;
    }

    public override void ReceiveData(NetPacket packet, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, CommandType commandType)
    {
        // Nothing
    }
}
