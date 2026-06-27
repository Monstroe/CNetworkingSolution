using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

namespace Monstroe.CNetworkingSolution
{
    public class ConnectionErrorEventBus : EventBus<ConnectionErrorEvent, ConnectionErrorEventResult, ConnectionErrorEventAttribute, ConnectionErrorEventHandler>
    {
        internal override ConnectionErrorEventHandler CreateEventHandler(INetEvent listener, Type eventType, Type returnType, MethodInfo method, ConnectionErrorEventAttribute attr)
        {
            return new ConnectionErrorEventHandler
            {
                Listener = listener,
                Invoke = (e) => (Task<ConnectionErrorEventResult>)method.Invoke(listener, new object[] { e }),
                EventType = eventType,
                ReturnType = returnType,
                EventPriority = attr.Priority
            };
        }

        internal override async Task<ConnectionErrorEventResult> HandleEvents(ConnectionErrorEvent e, List<ConnectionErrorEventHandler> handlers)
        {
            ConnectionErrorEventResult finalResult = ConnectionErrorEventResult.Continue();
            finalResult.Code = e.Code;
            finalResult.SocketError = e.SocketError;

            foreach (var handler in handlers)
            {
                _ = await handler.Invoke(e);
            }

            return finalResult;
        }
    }

    public class ConnectionErrorEvent : NetEvent
    {
        public TransportCode Code { get; internal set; }
        public SocketError? SocketError { get; internal set; }

        internal ConnectionErrorEvent() { }
    }

    public class ConnectionErrorEventResult : NetEventResult
    {
        public TransportCode Code { get; internal set; }
        public SocketError? SocketError { get; internal set; }

        private ConnectionErrorEventResult() { }

        public static ConnectionErrorEventResult Continue() => new ConnectionErrorEventResult();
    }

    public class ConnectionErrorEventHandler : NetEventHandler<ConnectionErrorEvent, ConnectionErrorEventResult> { }
}
