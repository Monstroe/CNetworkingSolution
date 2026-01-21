using UnityEngine;
using UnityEngine.Events;

public class EventManager : MonoBehaviour
{
    [HideInInspector] public static UnityEvent<EntityHealthChangeArgs> EntityHealthChange = new();
    [HideInInspector] public static UnityEvent<EntityJumpArgs> EntityJump = new();
    // ADD MORE EVENTS HERE
}

public class EntityHealthChangeArgs
{
    public ServerEntity Entity;
    public float HealthChange;
}

public class EntityJumpArgs
{
    public ServerEntity Entity;
    public float JumpHeight;
    public bool Canceled;
}

// ADD MORE ARGS HERE
