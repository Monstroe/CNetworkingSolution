using UnityEngine;

public class ServerInitializer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
#if CNS_TRANSPORT_CNET
        ServerManager.Instance.RegisterTransport<CNetTransport>();
#endif
#if CNS_TRANSPORT_LITENETLIB
        ServerManager.Instance.RegisterTransport<LiteNetLibTransport>();
#endif
    }
}
