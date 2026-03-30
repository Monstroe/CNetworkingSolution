using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ConnectionEventAttribute : EventAttribute
{
    public bool IgnoreDenied { get; }

    public ConnectionEventAttribute(EventPriority priority = EventPriority.Normal, bool ignoreDenied = false) : base(priority)
    {
        IgnoreDenied = ignoreDenied;
    }
}
