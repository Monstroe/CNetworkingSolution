using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class EventHandlerAttribute : Attribute
{
    public EventPriority Priority { get; } = EventPriority.Normal;
    public bool IgnoreCancelled { get; } = false;
}
