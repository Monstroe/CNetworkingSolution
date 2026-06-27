using System.Collections.Generic;

namespace Monstroe.CNetworkingSolution
{
    public class TransportData
    {
        public NetDeviceType DeviceType { get; set; }

        private readonly List<uint> connectedClientIds = new List<uint>();
        public IReadOnlyList<uint> ConnectedClientIds => connectedClientIds;

        internal TransportData() { }

        public void AddConnectedClient(uint clientId)
        {
            if (!connectedClientIds.Contains(clientId))
            {
                connectedClientIds.Add(clientId);
            }
        }

        public bool RemoveConnectedClient(uint clientId)
        {
            return connectedClientIds.Remove(clientId);
        }
    }

    public enum NetDeviceType
    {
        Client,
        Server
    }
}