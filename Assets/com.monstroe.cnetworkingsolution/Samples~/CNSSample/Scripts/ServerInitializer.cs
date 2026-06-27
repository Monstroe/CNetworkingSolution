using UnityEngine;
using Monstroe.CNetworkingSolution;

public class ServerInitializer : MonoBehaviour
{
    [SerializeField] private TransportType transportType = TransportType.CNet;
    private ServerManager serverManager;

    void Start()
    {
        serverManager = FindFirstObjectByType<ServerManager>();
        switch (transportType)
        {
            case TransportType.CNet:
                serverManager.RegisterTransport<CNetTransport>();
                break;
            case TransportType.LiteNetLib:
                serverManager.RegisterTransport<LiteNetLibTransport>();
                break;
        }
        serverManager.StartTransports();
    }
}

public enum TransportType
{
    CNet,
    LiteNetLib
}
