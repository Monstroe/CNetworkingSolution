using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class ServerEntity : ServerTransform
{
    public float MaxHealth { get { return maxHealth; } set { maxHealth = value; } }
    public float Health { get { return health; } set { health = value; } }

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float health = 100f;

    public virtual void UpdateHealth(float delta, ServerEntity changer = null)
    {
        EntityHealthChangeEvent evt = new EntityHealthChangeEvent
        {
            Entity = this,
            HealthChange = delta
        };
        lobby.GetService<EventServerSerivce>().Fire(evt);

        if (evt.Cancelled)
        {
            return;
        }

        health = Mathf.Clamp(health + evt.HealthChange, 0, maxHealth);
        if (health <= 0)
        {
            Die(changer);
        }
    }

    public virtual void Die(ServerEntity killer = null)
    {
        if (killer != null)
        {
            EntityKilledEntityEvent killEvt = new EntityKilledEntityEvent()
            {
                Killer = killer,
                Killed = this
            };
            lobby.GetService<EventServerSerivce>().Fire(killEvt);

            if (killEvt.Cancelled)
            {
                return;
            }
        }

        EntityDiedEvent dieEvt = new EntityDiedEvent()
        {
            Entity = this
        };
        lobby.GetService<EventServerSerivce>().Fire(dieEvt);
        if (dieEvt.Cancelled)
        {
            return;
        }

        DestroyOnServer(this);
    }
}

public class EntityHealthChangeEvent : GameEvent
{
    public ServerEntity Entity;
    public float HealthChange;
}

public class EntityJumpEvent : GameEvent
{
    public ServerEntity Entity;
    public float JumpHeight;
}

public class EntityGroundHitEvent : GameEvent
{
    public ServerEntity Entity;
}

public class EntityKilledEntityEvent : GameEvent
{
    public ServerEntity Killer;
    public ServerEntity Killed;
}

public class EntityDiedEvent : GameEvent
{
    public ServerEntity Entity;
}
