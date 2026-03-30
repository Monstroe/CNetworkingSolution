using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class GameEventAttribute : EventAttribute
{
    public bool IgnoreCancelled { get; }

    public GameEventAttribute(EventPriority priority = EventPriority.Normal, bool ignoreCancelled = false) : base(priority)
    {
        IgnoreCancelled = ignoreCancelled;
    }
}
