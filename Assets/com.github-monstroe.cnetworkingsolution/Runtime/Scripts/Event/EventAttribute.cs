using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EventAttribute : Attribute
{
    public EventPriority Priority { get; }

    public EventAttribute(EventPriority priority = EventPriority.Normal)
    {
        Priority = priority;
    }
}
