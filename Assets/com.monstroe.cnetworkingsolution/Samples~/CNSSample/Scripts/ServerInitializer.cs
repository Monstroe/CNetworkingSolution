using UnityEngine;
using Monstroe.CNetworkingSolution;

public class ServerInitializer : MonoBehaviour
{
    [SerializeField] private TransportType transportType = TransportType.CNet;

    void Start()
    {
        switch (transportType)
        {
            case TransportType.CNet:
                NetworkManager.Instance.Server.RegisterTransport<CNetTransport>();
                break;
            case TransportType.LiteNetLib:
                NetworkManager.Instance.Server.RegisterTransport<LiteNetLibTransport>();
                break;
        }
        NetworkManager.Instance.Server.StartTransports();
    }
}

public enum TransportType
{
    CNet,
    LiteNetLib
}
