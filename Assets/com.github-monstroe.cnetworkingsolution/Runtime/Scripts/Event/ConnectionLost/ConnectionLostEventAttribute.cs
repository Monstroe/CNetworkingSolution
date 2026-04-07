using System;

namespace CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ConnectionLostEventAttribute : EventAttribute
    {
        public ConnectionLostEventAttribute(EventPriority priority = EventPriority.Normal) : base(priority) { }
    }
}
