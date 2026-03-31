using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DisconnectionEventAttribute : EventAttribute
{
    public DisconnectionEventAttribute(EventPriority priority = EventPriority.Normal) : base(priority) { }
}
