using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

public class DisconnectionEventBus : EventBus<DisconnectionEvent, DisconnectionEventResult, DisconnectionEventAttribute, DisconnectionEventHandler>
{
    internal override DisconnectionEventHandler CreateEventHandler(INetEvent listener, Type eventType, Type returnType, MethodInfo method, DisconnectionEventAttribute attr)
    {
        return new DisconnectionEventHandler
        {
            Listener = listener,
            Invoke = (e) => (Task<DisconnectionEventResult>)method.Invoke(listener, new object[] { e }),
            EventType = eventType,
            ReturnType = returnType,
            EventPriority = attr.Priority
        };
    }

    internal override async Task<DisconnectionEventResult> HandleEvents(DisconnectionEvent e, List<DisconnectionEventHandler> handlers)
    {
        DisconnectionEventResult finalResult = DisconnectionEventResult.Continue();
        finalResult.DisconnectingUser = e.DisconnectingUser;

        foreach (var handler in handlers)
        {
            _ = await handler.Invoke(e);
        }

        return finalResult;
    }
}

public abstract class DisconnectionEvent : NetEvent
{
    public UserData DisconnectingUser { get; internal set; }
}

public class DisconnectionEventResult : NetEventResult
{
    public UserData DisconnectingUser { get; internal set; }

    private DisconnectionEventResult() { }

    public static DisconnectionEventResult Continue() => new DisconnectionEventResult();
}

public class DisconnectionEventHandler : NetEventHandler<DisconnectionEvent, DisconnectionEventResult> { }
