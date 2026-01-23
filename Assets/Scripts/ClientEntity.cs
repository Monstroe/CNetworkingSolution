using System.Net;
using UnityEngine;

public abstract class ClientEntity : ClientTransform
{
    public float MaxHealth { get { return maxHealth; } set { maxHealth = value; } }
    public float Health { get { return health; } set { health = value; } }

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float health = 100f;

    public override void Init(ushort id, ClientLobby lobby)
    {
        base.Init(id, lobby);
        lobby.GetService<EntityClientService>().ClientEntities.Add(id, this);
    }

    [Rpc]
    public virtual void UpdateHealth(float delta, ushort? changerId = null)
    {
        Debug.Log($"CLIENT: Entity {Id} Health changing by {delta}, new health before clamp {health + delta}");
        health = Mathf.Clamp(health + delta, 0, maxHealth);
    }

    [Rpc]
    public virtual void Die(ushort? killerId = null)
    {
        Debug.Log($"CLIENT: Entity {Id} Died");
    }
}
