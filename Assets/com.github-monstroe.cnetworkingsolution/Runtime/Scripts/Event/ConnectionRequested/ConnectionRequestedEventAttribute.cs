using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ConnectionRequestedEventAttribute : EventAttribute
{
    public bool IgnoreRejected { get; }

    public ConnectionRequestedEventAttribute(EventPriority priority = EventPriority.Normal, bool ignoreRejected = false) : base(priority)
    {
        IgnoreRejected = ignoreRejected;
    }
}
