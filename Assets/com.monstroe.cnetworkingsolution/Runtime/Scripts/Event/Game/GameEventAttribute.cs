using System;

namespace Monstroe.CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class GameEventAttribute : EventAttribute
    {
        public bool IgnoreCancelled { get; }

        public GameEventAttribute(EventPriority priority = EventPriority.Normal, bool ignoreCancelled = false) : base(priority)
        {
            IgnoreCancelled = ignoreCancelled;
        }
    }
}
