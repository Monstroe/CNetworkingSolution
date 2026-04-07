using System.Collections.Generic;
using System.Net;

namespace CNetworkingSolution
{
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
        public void StartTransports();
        public void RegisterTransport<T>(NetDeviceType deviceType) where T : NetTransport;
        public void AddTransport(NetTransport transport);
        public void DisconnectTransports();
        public void RemoveTransport(NetTransport transport);
        public void RemoveTransports();
    }
}