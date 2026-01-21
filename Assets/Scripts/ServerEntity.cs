using UnityEngine;

public class ServerEntity : ServerTransform
{
    public float MaxHealth { get { return maxHealth; } set { maxHealth = value; } }
    public float Health { get { return health; } set { health = value; } }

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float health = 100f;
}
