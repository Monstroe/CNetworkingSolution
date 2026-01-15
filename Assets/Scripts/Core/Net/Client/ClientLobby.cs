using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientLobby : MonoBehaviour
{
    public LobbyData LobbyData { get; set; } = new LobbyData();
    public UserData CurrentUser { get; set; } = new UserData();
    public ulong ClientTick { get; set; } = 0;

    private ITransportUtility transportUtility;
    private readonly ClientServiceUtility services = new ClientServiceUtility();
    private readonly ClientServiceUtility unconnectedServices = new ClientServiceUtility();

    public void Init(ITransportUtility transport)
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

    public void SendToUnconnected(IPEndPoint iPEndPoint, NetPacket packet)
    {
        if (packet != null)
        {
            transportUtility.SendToUnconnectedRemote(iPEndPoint, packet);
        }
    }

    public void SendToUnconnected(List<IPEndPoint> iPEndPoints, NetPacket packet)
    {
        if (packet != null)
        {
            transportUtility.SendToUnconnectedRemotes(iPEndPoints, packet);
        }
    }

    public void BroadcastToUnconnected(NetPacket packet)
    {
        if (packet != null)
        {
            transportUtility.BroadcastToUnconnectedRemotes(packet);
        }
    }

    public void DisconnectFromLobby()
    {
        transportUtility.DisconnectTransports();
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

    public void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet)
    {
        ServiceType serviceType = (ServiceType)packet.ReadByte();
        CommandType commandType = (CommandType)packet.ReadByte();

        if (unconnectedServices.GetService(serviceType, out ClientService unconnectedService))
        {
            unconnectedService.ReceiveDataUnconnected(ipEndPoint, packet, serviceType, commandType);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No unconnected service found for type {serviceType}. Command {commandType} will not be processed.");
        }
    }

    public void RegisterService<T>(T service) where T : ClientService
    {
        if (services.RegisterService(service))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Registered ClientService {service.ServiceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {service.ServiceType} is already registered.");
        }
    }

    public void UnregisterService<T>() where T : ClientService
    {
        if (services.UnregisterService<T>(out ServiceType serviceType))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Unregistered ClientService {serviceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {serviceType} is not registered.");
        }
    }

    public T GetService<T>() where T : ClientService
    {
        ClientService service = services.GetService<T>(out ServiceType serviceType);
        if (service != null)
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {serviceType} not found.");
            return null;
        }
    }

    public void RegisterUnconnectedService<T>(T service) where T : ClientService
    {
        if (unconnectedServices.RegisterService(service))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Registered unconnected ClientService {service.ServiceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ClientService {service.ServiceType} is already registered.");
        }
    }

    public void UnregisterUnconnectedService<T>() where T : ClientService
    {
        if (unconnectedServices.UnregisterService<T>(out ServiceType serviceType))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: Unregistered unconnected ClientService {serviceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ClientService {serviceType} is not registered.");
        }
    }

    public T GetUnconnectedService<T>() where T : ClientService
    {
        ClientService service = unconnectedServices.GetService<T>(out ServiceType serviceType);
        if (service != null)
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ClientService {serviceType} not found.");
            return null;
        }
    }
}
