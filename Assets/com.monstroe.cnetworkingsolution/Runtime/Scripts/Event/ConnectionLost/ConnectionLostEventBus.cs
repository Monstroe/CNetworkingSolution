using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Monstroe.CNetworkingSolution
{
    public class ConnectionLostEventBus : EventBus<ConnectionLostEvent, ConnectionLostEventResult, ConnectionLostEventAttribute, ConnectionLostEventHandler>
    {
        internal override ConnectionLostEventHandler CreateEventHandler(INetEvent listener, Type eventType, Type returnType, MethodInfo method, ConnectionLostEventAttribute attr)
        {
            return new ConnectionLostEventHandler
            {
                Listener = listener,
                Invoke = (e) => (Task<ConnectionLostEventResult>)method.Invoke(listener, new object[] { e }),
                EventType = eventType,
                ReturnType = returnType,
                EventPriority = attr.Priority
            };
        }

        internal override async Task<ConnectionLostEventResult> HandleEvents(ConnectionLostEvent e, List<ConnectionLostEventHandler> handlers)
        {
            ConnectionLostEventResult finalResult = ConnectionLostEventResult.Continue();
            finalResult.DisconnectingUser = e.DisconnectingUser;

            foreach (var handler in handlers)
            {
                _ = await handler.Invoke(e);
            }

            return finalResult;
        }
    }

    public class ConnectionLostEvent : NetEvent
    {
        public UserData DisconnectingUser { get; internal set; }

        internal ConnectionLostEvent() { }
    }

    public class ConnectionLostEventResult : NetEventResult
    {
        public UserData DisconnectingUser { get; internal set; }

        private ConnectionLostEventResult() { }

        public static ConnectionLostEventResult Continue() => new ConnectionLostEventResult();
    }

    public class ConnectionLostEventHandler : NetEventHandler<ConnectionLostEvent, ConnectionLostEventResult> { }
}
