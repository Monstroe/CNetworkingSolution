using System;

namespace Monstroe.CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ServerVarRpcAttribute : ServerRpcAttribute
    {
        public SyncType SyncType { get; }

        public ServerVarRpcAttribute(SyncType syncType, TransportMethod transportMethod = TransportMethod.Reliable) : base(transportMethod)
        {
            SyncType = syncType;
        }
    }

    public enum SyncType
    {
        /// <summary>
        /// Syncs the variable to all clients in the lobby, including the client that owns the variable
        /// </summary>
        Lobby,

        /// <summary>
        /// Syncs the variable to all clients in the lobby, except the client that owns the variable
        /// </summary>
        LobbyExceptSelf,

        /// <summary>
        /// Syncs the variable to all clients in the game, including the client that owns the variable
        /// </summary>
        Game,

        /// <summary>
        /// Syncs the variable to all clients in the game, except the client that owns the variable
        /// </summary>
        GameExceptSelf,

        /// <summary>
        /// Syncs the variable to the client that owns the variable
        /// </summary>
        User,

        /// <summary>
        /// Syncs the variable to no clients
        /// </summary>
        None
    }
}
