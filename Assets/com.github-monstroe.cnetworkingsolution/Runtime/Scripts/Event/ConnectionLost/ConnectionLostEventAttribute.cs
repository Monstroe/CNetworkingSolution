using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ConnectionLostEventAttribute : EventAttribute
{
    public ConnectionLostEventAttribute(EventPriority priority = EventPriority.Normal) : base(priority) { }
}
