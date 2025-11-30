using UnityEngine;

public class TransportData
{
    public string ConnectionAddress { get => connectionAddress; set => connectionAddress = value; }
    public ushort ConnectionPort { get => connectionPort; set => connectionPort = value; }
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
    public ulong SteamCode { get => steamCode; set => steamCode = value; }
#endif

    [SerializeField] private string connectionAddress;
    [SerializeField] private ushort connectionPort;
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
    [SerializeField] private ulong steamCode;
#endif
}
