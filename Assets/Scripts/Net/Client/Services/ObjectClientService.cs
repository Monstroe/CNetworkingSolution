using UnityEngine;

public class ObjectClientService : ClientService
{
    void Start()
    {
        ClientManager.Instance.CurrentLobby.RegisterService(ServiceType.OBJECT, this);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_COMMUNICATION:
                {
                    ushort objectId = packet.ReadUShort();
                    ServiceType objectServiceType = (ServiceType)packet.ReadByte();
                    CommandType objectCommand = (CommandType)packet.ReadByte();
                    ClientManager.Instance.CurrentLobby.GameData.ClientObjects.TryGetValue(objectId, out ClientObject clientObject);
                    if (clientObject != null)
                    {
                        clientObject.ReceiveData(packet, objectServiceType, objectCommand, transportMethod);
                    }
                    else
                    {
                        Debug.LogWarning($"ClientObject with ID {objectId} not found.");
                    }
                    break;
                }
        }
    }
}
