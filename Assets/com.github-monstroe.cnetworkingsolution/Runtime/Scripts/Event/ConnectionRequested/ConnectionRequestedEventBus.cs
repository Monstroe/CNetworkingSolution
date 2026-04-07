using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace CNetworkingSolution
{
    public class ConnectionRequestedEventBus : EventBus<ConnectionRequestedEvent, ConnectionRequestedEventResult, ConnectionRequestedEventAttribute, ConnectionRequestedEventHandler>
    {
        internal override ConnectionRequestedEventHandler CreateEventHandler(INetEvent listener, Type eventType, Type returnType, MethodInfo method, ConnectionRequestedEventAttribute attr)
        {
            return new ConnectionRequestedEventHandler
            {
                Listener = listener,
                Invoke = (e) => (Task<ConnectionRequestedEventResult>)method.Invoke(listener, new object[] { e }),
                EventType = eventType,
                ReturnType = returnType,
                EventPriority = attr.Priority,
                IgnoreRejected = attr.IgnoreRejected
            };
        }

        internal override async Task<ConnectionRequestedEventResult> HandleEvents(ConnectionRequestedEvent e, List<ConnectionRequestedEventHandler> handlers)
        {
            ConnectionRequestedEventResult finalResult = e.UserRejected ? ConnectionRequestedEventResult.Reject() : ConnectionRequestedEventResult.Accept();
            finalResult.ConnectingUser = e.ConnectingUser;
            finalResult.ConnectionTime = e.ConnectionTime;
            finalResult.ResponsePacket = e.ResponsePacket;

            foreach (var handler in handlers)
            {
                if (finalResult.UserRejected && !handler.IgnoreRejected)
                {
                    continue;
                }

                var result = await handler.Invoke(e);
                if (result.UserRejected)
                {
                    finalResult = ConnectionRequestedEventResult.Reject();
                    e.UserRejected = true;
                }
            }

            finalResult.ResponsePacket = e.ResponsePacket;
            return finalResult;
        }
    }

    public class ConnectionRequestedEvent : NetEvent
    {
        public UserData ConnectingUser { get; internal set; }
        public DateTime ConnectionTime { get; internal set; }
        public bool UserRejected { get; internal set; } = false;
        public NetPacket ResponsePacket { get; internal set; } = new NetPacket();

        internal ConnectionRequestedEvent() { }
    }

    public class ConnectionRequestedEventResult : NetEventResult
    {
        public UserData ConnectingUser { get; internal set; }
        public DateTime ConnectionTime { get; internal set; }
        public bool UserRejected { get; }
        public NetPacket ResponsePacket { get; internal set; }

        private ConnectionRequestedEventResult(bool rejectUser)
        {
            UserRejected = rejectUser;
        }

        public static ConnectionRequestedEventResult Accept() => new ConnectionRequestedEventResult(false);
        public static ConnectionRequestedEventResult Reject() => new ConnectionRequestedEventResult(true);
    }

    public class ConnectionRequestedEventHandler : NetEventHandler<ConnectionRequestedEvent, ConnectionRequestedEventResult>
    {
        public bool IgnoreRejected;
    }
}