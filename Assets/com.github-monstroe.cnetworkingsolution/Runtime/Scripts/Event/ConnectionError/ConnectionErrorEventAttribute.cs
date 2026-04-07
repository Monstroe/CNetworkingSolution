using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ConnectionErrorEventAttribute : EventAttribute
{
    public ConnectionErrorEventAttribute(EventPriority priority = EventPriority.Normal) : base(priority) { }
}
