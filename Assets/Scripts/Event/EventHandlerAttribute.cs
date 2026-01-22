using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EventHandlerAttribute : Attribute
{
    public EventPriority Priority { get; }
    public bool IgnoreCancelled { get; }

    public EventHandlerAttribute(EventPriority priority = EventPriority.Normal, bool ignoreCancelled = false)
    {
        Priority = priority;
        IgnoreCancelled = ignoreCancelled;
    }
}
