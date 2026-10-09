using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Monstroe.CNetworkingSolution
{
    public class GameEventBus : EventBus<GameEvent, GameEventResult, GameEventAttribute, GameEventHandler>
    {
        internal override GameEventHandler CreateEventHandler(INetEvent listener, Type eventType, Type returnType, MethodInfo method, GameEventAttribute attr)
        {
            return new GameEventHandler
            {
                Listener = listener,
                Invoke = async e =>
                {
                    var result = method.Invoke(listener, new object[] { e });

                    if (result is GameEventResult eventResult)
                        return eventResult;

                    if (result is Task<GameEventResult> task)
                        return await task;

                    throw new InvalidOperationException($"The method {method.Name} on {listener.GetType().Name} must return either a Task<GameEventResult> or a GameEventResult");
                },
                EventType = eventType,
                ReturnType = returnType,
                EventPriority = attr.Priority,
                IgnoreCancelled = attr.IgnoreCancelled
            };
        }

        internal override async Task<GameEventResult> HandleEvents(GameEvent e, List<GameEventHandler> handlers)
        {
            GameEventResult finalResult = e.Canceled ? GameEventResult.Cancel() : GameEventResult.Continue();

            foreach (var handler in handlers)
            {
                if (finalResult.Canceled && !handler.IgnoreCancelled)
                {
                    continue;
                }

                var result = await handler.Invoke(e);
                if (result.Canceled)
                {
                    finalResult = GameEventResult.Cancel();
                    e.Canceled = true;
                }
            }

            return finalResult;
        }
    }

    public abstract class GameEvent : NetEvent
    {
        public bool Canceled { get; internal set; } = false;
    }

    public class GameEventResult : NetEventResult
    {
        public bool Canceled { get; }

        private GameEventResult(bool cancel)
        {
            Canceled = cancel;
        }

        public static GameEventResult Continue() => new GameEventResult(false);
        public static GameEventResult Cancel() => new GameEventResult(true);
    }

    public class GameEventHandler : NetEventHandler<GameEvent, GameEventResult>
    {
        public bool IgnoreCancelled;
    }
}
