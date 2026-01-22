using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using UnityEngine;

public class EventServerSerivce : ServerService
{
    public EventBus Bus { get; private set; } = new EventBus();

    public override void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        // Nothing
    }

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }

    public override void Tick()
    {
        // Nothing
    }

    public override void UserJoined(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserJoinedGame(UserData joinedUser)
    {
        // Nothing
    }

    public override void UserLeft(UserData leftUser)
    {
        // Nothing
    }
}

public abstract class GameEvent
{
    public bool Cancelled = false;
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
