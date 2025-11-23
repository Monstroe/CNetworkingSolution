using UnityEngine;

public class ClientLobby : MonoBehaviour
{
    public LobbyData LobbyData { get; set; } = new LobbyData();
    public UserData CurrentUser { get; set; }
    public ulong ClientTick { get; set; } = 0;

    private SingleTransportUtility transportUtility;
    private readonly ClientServiceUtility services = new ClientServiceUtility();

    public void Init(SingleTransportUtility transport)
    {
        transportUtility = transport;

        foreach (var service in this.GetComponentsInChildren<ClientService>())
        {
            service.Init(this);
        }
    }

    void FixedUpdate()
    {
        ClientTick++;
    }

    public void SendToServer(NetPacket packet, TransportMethod method)
    {
        if (packet != null)
        {
            transportUtility.SendToAllRemotes(packet, method);
        }
    }

    public void DisconnectFromLobby()
    {
        transportUtility.RemoveTransport();
    }

    public void ReceiveData(NetPacket packet, TransportMethod? transportMethod)
    {
        ServiceType serviceType = (ServiceType)packet.ReadByte();
        CommandType commandType = (CommandType)packet.ReadByte();

        if (services.GetService(serviceType, out ClientService service))
        {
            service.ReceiveData(packet, serviceType, commandType, transportMethod);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No service found for type {serviceType}. Command {commandType} will not be processed.");
        }
    }

    public void RegisterService<T>(T service) where T : ClientService
    {
        if (services.RegisterService(service))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Registered ClientService {typeof(T)}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {typeof(T)} is already registered.");
        }
    }

    public void UnregisterService<T>() where T : ClientService
    {
        if (services.UnregisterService<T>())
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Unregistered ClientService {typeof(T)}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {typeof(T)} is not registered.");
        }
    }

    public T GetService<T>() where T : ClientService
    {
        ClientService service = services.GetService<T>();
        if (service != null)
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {typeof(T)} not found.");
            return null;
        }
    }
}
