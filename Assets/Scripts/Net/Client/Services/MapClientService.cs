using System.Collections.Generic;
using UnityEngine;

public class MapClientService : ClientService
{

    void Start()
    {
        ClientManager.Instance.CurrentLobby.RegisterService(ServiceType.MAP, this);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.MAP_OBJECTS_INIT:
                {
                    ushort[] startingObjectIds = packet.ReadUShorts();
                    List<ClientObject> startingClientObjects = GameContent.Instance.Map.GetStartingClientObjects();
                    for (int i = 0; i < startingClientObjects.Count; i++)
                    {
                        var obj = startingClientObjects[i];
                        obj.Init(startingObjectIds[i]);
                        ClientManager.Instance.CurrentLobby.GameData.ClientObjects.Add(obj.Id, obj);
                    }
                    break;
                }
        }
    }
}
