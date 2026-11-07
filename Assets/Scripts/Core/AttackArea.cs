using System;
using UnityEngine;

public class AttackArea : MonoBehaviour
{
    [SerializeField] public int damage;
    [SerializeField] public FactionId factionId;

    public event Action Attack;

    public void InitializeAttackArea(FactionId designatedFactionId, int designatedDamage)
    {
        factionId = designatedFactionId;
        damage = designatedDamage;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Enemy base arrivals are resolved by the creep owner, never by fake lethal damage.
        CreepController creep = GetComponentInParent<CreepController>();
        if (creep != null)
        {
            BaseController playerBase = collision.GetComponentInParent<BaseController>();
            if (playerBase != null) creep.ReachBase();
            return;
        }

        HealthController healthController = collision.GetComponent<HealthController>();

        if (healthController != null && healthController.GetFactionId() != factionId)
        {
            healthController.GetDamaged(transform.parent.gameObject, damage);
            Attack?.Invoke();

            Destroy(gameObject);
        }
    }
}
