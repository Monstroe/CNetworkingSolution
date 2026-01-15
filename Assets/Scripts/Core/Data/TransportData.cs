using System;
using System.Collections.Generic;
using UnityEngine;

public class TransportData
{
    public NetDeviceType DeviceType { get; set; }
    public TransportType TransportType { get; set; }
    public virtual List<uint> ConnectedClientIds { get; set; } = new List<uint>();
    public TransportSettings Settings { get; set; } = new TransportSettings();
}

[Serializable]
public class TransportSettings : IDeepClone<TransportSettings>
{
#nullable enable

#if CNS_TRANSPORT_LITENETLIB || CNS_TRANSPORT_CNET
    public string? ConnectionAddress { get => connectionAddress; set => connectionAddress = value; }
    public ushort? ConnectionPort { get => connectionPort; set => connectionPort = value ?? 0; }
    public string? ConnectionKey { get => connectionKey; set => connectionKey = value; }
#endif
#if CNS_TRANSPORT_LITENETLIB
    public bool? UnconnectedPacketsEnabled { get => unconnectedPacketsEnabled; set => unconnectedPacketsEnabled = value ?? false; }
#endif
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
    public ulong? SteamCode { get => steamCode; set => steamCode = value ?? 0; }
#endif

#if CNS_TRANSPORT_LITENETLIB || CNS_TRANSPORT_CNET
    [SerializeField] private string? connectionAddress;
    [SerializeField] private ushort connectionPort;
    [SerializeField] private string? connectionKey;
#endif
#if CNS_TRANSPORT_LITENETLIB
    [SerializeField] private bool unconnectedPacketsEnabled;
#endif
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
    [SerializeField] private ulong steamCode;
#endif
#nullable disable

    public TransportSettings Clone()
    {
        return new TransportSettings()
        {
            ConnectionAddress = this.ConnectionAddress,
            ConnectionPort = this.ConnectionPort,
            ConnectionKey = this.ConnectionKey,
            UnconnectedPacketsEnabled = this.UnconnectedPacketsEnabled,
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
            SteamCode = this.SteamCode
#endif
        };
    }
}

public enum NetDeviceType
{
    Client,
    Server
}

public enum TransportType
{
#if CNS_TRANSPORT_LOCAL
    Local,
#endif
#if CNS_TRANSPORT_CNET && CNS_TRANSPORT_CNETRELAY && CNS_TRANSPORT_LOCAL && CNS_SYNC_HOST && CNS_LOBBY_MULTIPLE
    CNetRelay,
#endif
#if CNS_TRANSPORT_CNET && CNS_TRANSPORT_CNETBROADCAST
    CNetBroadcast,
#endif
#if CNS_TRANSPORT_CNET
    CNet,
#endif
#if CNS_TRANSPORT_LITENETLIB && CNS_TRANSPORT_LITENETLIBRELAY && CNS_TRANSPORT_LOCAL && CNS_SYNC_HOST && CNS_LOBBY_MULTIPLE
    LiteNetLibRelay,
#endif
#if CNS_TRANSPORT_LITENETLIB && CNS_TRANSPORT_LITENETLIBBROADCAST
    LiteNetLibBroadcast,
#endif
#if CNS_TRANSPORT_LITENETLIB
    LiteNetLib,
#endif
#if CNS_TRANSPORT_STEAMRELAY && CNS_SYNC_HOST
    SteamRelay,
#endif
}