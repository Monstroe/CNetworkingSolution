using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace CNetworkingSolution
{
    public abstract class EventBus<T1, T2, T3, T4> where T1 : NetEvent where T2 : NetEventResult where T3 : EventAttribute where T4 : NetEventHandler<T1, T2>
    {
        private readonly Dictionary<Type, List<T4>> registeredEventHandlers = new Dictionary<Type, List<T4>>();
        private readonly Dictionary<INetEvent, List<T4>> listenerHandlerMap = new Dictionary<INetEvent, List<T4>>();
        private readonly Dictionary<Type, List<(MethodInfo, T3)>> listenerMethodCache = new Dictionary<Type, List<(MethodInfo, T3)>>();

        public void RegisterListener(INetEvent listener)
        {
            if (listenerHandlerMap.ContainsKey(listener))
            {
                throw new Exception($"Listener of type {listener.GetType().Name} is already registered.");
            }

            var type = listener.GetType();

            if (!listenerMethodCache.TryGetValue(type, out var methods))
            {
                methods = new List<(MethodInfo, T3)>();
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method.GetCustomAttribute<T3>() is T3 attr)
                    {
                        var parameters = method.GetParameters();
                        if (parameters.Length != 1 || !typeof(T1).IsAssignableFrom(parameters[0].ParameterType))
                        {
                            throw new Exception($"Method {type.Name}.{method.Name} has invalid signature for NetEventHandler.");
                        }

                        if (method.ReturnType != typeof(T2))
                        {
                            throw new Exception($"Method {type.Name}.{method.Name} has invalid return type for NetEventHandler.");
                        }
                        methods.Add((method, attr));
                    }
                }
                listenerMethodCache[type] = methods;
            }

            List<T4> newHandlers = new List<T4>();

            foreach (var (method, attr) in methods)
            {
                var eventType = method.GetParameters()[0].ParameterType;
                var returnType = method.ReturnType;

                var handler = CreateEventHandler(listener, eventType, returnType, method, attr);

                if (!registeredEventHandlers.TryGetValue(eventType, out var list))
                {
                    list = new List<T4>();
                    registeredEventHandlers[eventType] = list;
                }

                list.Add(handler);
                list.Sort((a, b) => a.EventPriority.CompareTo(b.EventPriority));

                newHandlers.Add(handler);
            }

            listenerHandlerMap[listener] = newHandlers;
        }

        internal abstract T4 CreateEventHandler(INetEvent listener, Type eventType, Type returnType, MethodInfo method, T3 attr);

        public void UnregisterListener(INetEvent listener)
        {
            if (!listenerHandlerMap.TryGetValue(listener, out List<T4> handlers))
            {
                throw new Exception($"Listener of type {listener.GetType().Name} is not registered.");
            }

            foreach (T4 handler in handlers)
            {
                Type eventType = handler.EventType;
                if (registeredEventHandlers.TryGetValue(eventType, out List<T4> list))
                {
                    list.Remove(handler);
                }
            }

            listenerHandlerMap.Remove(listener);
        }

        public async Task<T2> Fire(T1 e)
        {
            var eventType = e.GetType();
            registeredEventHandlers.TryGetValue(eventType, out List<T4> handlers);
            return await HandleEvents(e, handlers ?? new List<T4>());
        }

        internal abstract Task<T2> HandleEvents(T1 e, List<T4> handlers);
    }

    public abstract class NetEvent { }
    public abstract class NetEventResult { }

    public abstract class NetEventHandler<T1, T2> where T1 : NetEvent where T2 : NetEventResult
    {
        public object Listener;
        public Func<T1, Task<T2>> Invoke;
        public Type EventType;
        public Type ReturnType;
        public EventPriority EventPriority;

        internal NetEventHandler() { }
    }

    public enum EventPriority
    {
        /// <summary>
        /// Lowest priority, called first.
        /// </summary>
        Lowest = 0,
        /// <summary>
        /// Low priority, called second.
        /// </summary>
        Low = 1,
        /// <summary>
        /// Normal priority, called third (default).
        /// </summary>
        Normal = 2,
        /// <summary>
        /// High priority, called fourth.
        /// </summary>
        High = 3,
        /// <summary>
        /// Highest priority called fifth.
        /// </summary>
        Highest = 4,
        /// <summary>
        /// Monitor priority, called last (mainly for monitoring purposes).
        /// </summary>
        Monitor = 5
    }
}
