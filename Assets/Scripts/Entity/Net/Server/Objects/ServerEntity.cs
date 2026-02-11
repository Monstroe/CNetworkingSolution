using UnityEngine;

public class ServerEntity : ServerTransform
{
    public float MaxHealth { get { return maxHealth; } set { maxHealth = value; } }
    public float Health { get { return health; } set { health = value; } }

    [Header("ServerEntity Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float health = 100f;

    public override void Init(ushort id, ServerLobby lobby)
    {
        base.Init(id, lobby);
        lobby.GetService<EntityServerService>().ServerEntities.Add(id, this);
    }

    [Rpc]
    public virtual void UpdateHealth(float delta, ushort? changerId = null)
    {
        ServerEntity changer = changerId.HasValue ? lobby.GetService<EntityServerService>().ServerEntities[changerId.Value] : null;

        if (changer != null && delta < 0)
        {
            EntityDamagedEntityEvent dmgEvt = new EntityDamagedEntityEvent()
            {
                Damager = changer,
                Damaged = this,
                DamageAmount = -delta
            };
            lobby.GetService<ObjectServerService>().EventBus.Fire(dmgEvt);

            if (dmgEvt.Cancelled)
            {
                return;
            }

            delta = -dmgEvt.DamageAmount;
        }

        EntityHealthChangeEvent hpEvt = new EntityHealthChangeEvent
        {
            Entity = this,
            HealthChange = delta
        };
        lobby.GetService<ObjectServerService>().EventBus.Fire(hpEvt);

        if (hpEvt.Cancelled)
        {
            return;
        }

        delta = hpEvt.HealthChange;

        health = Mathf.Clamp(health + delta, 0, maxHealth);
        Debug.Log($"SERVER: Entity {Id} Health changing by {delta}, new health after clamp {health}");
        InvokeOnGameClientObjects(nameof(UpdateHealth), delta, changerId);

        if (health <= 0)
        {
            Die(changerId);
        }
    }

    [Rpc]
    public virtual void Die(ushort? killerId = null)
    {
        ServerEntity killer = killerId.HasValue ? lobby.GetService<EntityServerService>().ServerEntities[killerId.Value] : null;

        if (killer != null)
        {
            EntityKilledEntityEvent killEvt = new EntityKilledEntityEvent()
            {
                Killer = killer,
                Killed = this
            };
            lobby.GetService<ObjectServerService>().EventBus.Fire(killEvt);

            if (killEvt.Cancelled)
            {
                return;
            }
        }

        EntityDiedEvent dieEvt = new EntityDiedEvent()
        {
            Entity = this
        };
        lobby.GetService<ObjectServerService>().EventBus.Fire(dieEvt);
        if (dieEvt.Cancelled)
        {
            return;
        }

        InvokeOnGameClientObjects(nameof(Die), killerId);
        DestroyOnServer(this);
    }
}

public class EntityDamagedEntityEvent : GameEvent
{
    public ServerEntity Damager;
    public ServerEntity Damaged;
    public float DamageAmount;
}

public class EntityHealthChangeEvent : GameEvent
{
    public ServerEntity Entity;
    public float HealthChange;
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
