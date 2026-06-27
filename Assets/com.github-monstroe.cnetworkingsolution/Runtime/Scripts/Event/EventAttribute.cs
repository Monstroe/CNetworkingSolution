using System;

namespace Monstroe.CNetworkingSolution
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public abstract class EventAttribute : Attribute
    {
        public EventPriority Priority { get; }

        public EventAttribute(EventPriority priority = EventPriority.Normal)
        {
            Priority = priority;
        }
    }
}
