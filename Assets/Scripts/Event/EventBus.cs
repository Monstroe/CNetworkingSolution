using System;
using System.Collections.Generic;
using System.Reflection;

public static class EventBus
{
    private class EventHandler
    {
        public object Listener;
        public Action<GameEvent> Invoke;
        public Type EventType;
        public EventPriority EventPriority;
        public bool IgnoreCancelled;
    }

    private static readonly Dictionary<Type, List<EventHandler>> registeredEventHandlers = new();
    private static readonly Dictionary<object, List<EventHandler>> listenerHandlerMap = new Dictionary<object, List<EventHandler>>();
    private static readonly Dictionary<Type, List<(MethodInfo, EventHandlerAttribute)>> listenerMethodCache = new Dictionary<Type, List<(MethodInfo, EventHandlerAttribute)>>();

    public static void RegisterListener(object listener)
    {
        var type = listener.GetType();

        if (!listenerMethodCache.TryGetValue(type, out var methods))
        {
            methods = new List<(MethodInfo, EventHandlerAttribute)>();
            foreach (var method in type.GetMethods())
            {
                if (method.GetCustomAttribute<EventHandlerAttribute>() is EventHandlerAttribute attr)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != 1 || !typeof(GameEvent).IsAssignableFrom(parameters[0].ParameterType))
                    {
                        throw new Exception($"Method {type.Name}.{method.Name} has invalid signature for EventHandler.");
                    }

                    if (method.ReturnType != typeof(void))
                    {
                        throw new Exception($"Method {type.Name}.{method.Name} has invalid return type for EventHandler.");
                    }
                    methods.Add((method, attr));
                }
            }
            listenerMethodCache[type] = methods;
        }

        List<EventHandler> newHandlers = new List<EventHandler>();

        foreach (var (method, attr) in methods)
        {
            var eventType = method.GetParameters()[0].ParameterType;

            var handler = new EventHandler
            {
                Listener = listener,
                Invoke = (e) => method.Invoke(listener, new object[] { e }),
                EventType = eventType,
                EventPriority = attr.Priority,
                IgnoreCancelled = attr.IgnoreCancelled
            };

            if (!registeredEventHandlers.TryGetValue(eventType, out var list))
            {
                list = new List<EventHandler>();
                registeredEventHandlers[eventType] = list;
            }

            list.Add(handler);
            list.Sort((a, b) => a.EventPriority.CompareTo(b.EventPriority));

            newHandlers.Add(handler);
        }

        listenerHandlerMap[listener] = newHandlers;
    }

    public static void UnregisterListener(object listener)
    {
        if (!listenerHandlerMap.TryGetValue(listener, out List<EventHandler> handlers))
        {
            return;
        }

        foreach (EventHandler handler in handlers)
        {
            Type eventType = handler.EventType;
            if (registeredEventHandlers.TryGetValue(eventType, out List<EventHandler> list))
            {
                list.Remove(handler);
            }
        }

        listenerHandlerMap.Remove(listener);
    }

    public static void Fire(GameEvent e)
    {
        var eventType = e.GetType();

        if (!registeredEventHandlers.TryGetValue(eventType, out List<EventHandler> handlers))
        {
            return;
        }

        foreach (var handler in handlers)
        {
            if (e.Cancelled && !handler.IgnoreCancelled)
            {
                continue;
            }

            handler.Invoke(e);
        }
    }
}

public abstract class GameEvent
{
    public bool Cancelled = false;
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
