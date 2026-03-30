using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

public class ConnectionEventBus : EventBus<ConnectionEvent, ConnectionEventResult, ConnectionEventAttribute, ConnectionEventHandler>
{
    internal override ConnectionEventHandler CreateEventHandler(INetEvent listener, Type eventType, Type returnType, MethodInfo method, ConnectionEventAttribute attr)
    {
        return new ConnectionEventHandler
        {
            Listener = listener,
            Invoke = (e) => (Task<ConnectionEventResult>)method.Invoke(listener, new object[] { e }),
            EventType = eventType,
            ReturnType = returnType,
            EventPriority = attr.Priority,
            IgnoreDenied = attr.IgnoreDenied
        };
    }

    internal override async Task<ConnectionEventResult> HandleEvents(ConnectionEvent e, List<ConnectionEventHandler> handlers)
    {
        ConnectionEventResult finalResult = e.UserDenied ? ConnectionEventResult.Deny() : ConnectionEventResult.Allow();
        finalResult.PayloadPacket = e.PayloadPacket;

        foreach (var handler in handlers)
        {
            if (finalResult.UserDenied && !handler.IgnoreDenied)
            {
                continue;
            }

            var result = await handler.Invoke(e);
            if (result.UserDenied)
            {
                finalResult = ConnectionEventResult.Deny();
                e.UserDenied = true;
            }
        }

        finalResult.PayloadPacket = e.PayloadPacket;
        return finalResult;
    }
}

public abstract class ConnectionEvent : NetEvent
{
    public bool UserDenied { get; internal set; } = false;
    public NetPacket PayloadPacket { get; internal set; } = new NetPacket();
}

public class ConnectionEventResult : NetEventResult
{
    public bool UserDenied { get; }
    public NetPacket PayloadPacket { get; internal set; }

    private ConnectionEventResult(bool denyUser)
    {
        UserDenied = denyUser;
    }

    public static ConnectionEventResult Allow() => new ConnectionEventResult(false);
    public static ConnectionEventResult Deny() => new ConnectionEventResult(true);
}

public class ConnectionEventHandler : NetEventHandler<ConnectionEvent, ConnectionEventResult>
{
    public bool IgnoreDenied;
}
