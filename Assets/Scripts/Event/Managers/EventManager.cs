using System;
using System.Collections.Generic;
using System.Reflection;

public static class EventBus
{
    private sealed class EventHandler
    {
        public Action<GameEvent> Invoke;
        public EventPriority Priority;
        public bool IgnoreCancelled;
    }

    private static readonly Dictionary<Type, List<EventHandler>> eventHandlers = new();

    public static void RegisterAllEventHandlers(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = method.GetCustomAttribute<EventHandlerAttribute>();
                if (attr == null)
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length != 1 || !typeof(GameEvent).IsAssignableFrom(parameters[0].ParameterType))
                {
                    throw new Exception($"Invalid EventHandler signature: {method.DeclaringType}.{method.Name}");
                }

                object instance = null;
                if (!method.IsStatic)
                {
                    instance = Activator.CreateInstance(method.DeclaringType);
                }

                Type eventType = parameters[0].ParameterType;

                Action<GameEvent> del = e => method.Invoke(instance, new object[] { e });

                if (!eventHandlers.TryGetValue(eventType, out var list))
                {
                    list = new List<EventHandler>();
                    eventHandlers[eventType] = list;
                }

                list.Add(new EventHandler
                {
                    Invoke = del,
                    Priority = attr.Priority,
                    IgnoreCancelled = attr.IgnoreCancelled
                });
            }
        }

        // Sort handlers per event by priority
        foreach (var list in eventHandlers.Values)
        {
            list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
    }

    public static void Fire(GameEvent e)
    {
        Type type = e.GetType();

        // Walk inheritance chain (Spigot behavior)
        while (type != null && type != typeof(object))
        {
            if (eventHandlers.TryGetValue(type, out var list))
            {
                DispatchToHandlers(e, list);
            }

            type = type.BaseType;
        }
    }

    private static void DispatchToHandlers(GameEvent e, List<EventHandler> list)
    {
        bool cancelled = e is ICancellable c && c.Cancelled;

        foreach (var handler in list)
        {
            if (cancelled && handler.IgnoreCancelled)
                continue;

            // Enforce MONITOR semantics if you want
            if (handler.Priority == EventPriority.Monitor && e is ICancellable mc && mc.Cancelled)
            {
                // MONITOR still runs, but must not mutate
            }

            handler.Invoke(e);

            if (e is ICancellable c2)
                cancelled = c2.Cancelled;
        }
    }
}

public abstract class GameEvent { }

public interface ICancellable
{
    bool Cancelled { get; set; }
}

public enum EventPriority
{
    Lowest = 0,
    Low = 1,
    Normal = 2,
    High = 3,
    Highest = 4,
    Monitor = 5
}
