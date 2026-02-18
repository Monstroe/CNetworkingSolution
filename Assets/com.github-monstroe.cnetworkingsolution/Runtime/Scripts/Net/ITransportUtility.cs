using System.Collections.Generic;
using System.Net;

public interface ITransportUtility
{
    public List<ulong> ConnectedUserIds { get; set; }

    public void SendToRemote(ulong remoteId, NetPacket packet, TransportMethod method);

    public void SendToRemotes(List<ulong> remoteIds, NetPacket packet, TransportMethod method);

    public void SendToAllRemotes(NetPacket packet, TransportMethod method);

    public void SendToUnconnectedRemote(IPEndPoint iPEndPoint, NetPacket packet);

    public void SendToUnconnectedRemotes(List<IPEndPoint> iPEndPoints, NetPacket packet);

    public void BroadcastToUnconnectedRemotes(NetPacket packet);

    public void KickRemote(ulong remoteId);
#nullable enable
    public void RegisterTransport(TransportType transportType, NetDeviceType deviceType, TransportSettings? transportSettings = null);
#nullable disable
    public void AddTransport(NetTransport transport);
    public void DisconnectTransports();
    public void RemoveTransport(NetTransport transport);
    public void RemoveTransports();
}
