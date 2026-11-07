using System.Collections.Generic;
using UnityEngine;

public class IcePillar : Projectile
{
    [SerializeField, Min(0f)] public float radius = 2f;
    [SerializeField] public SlowDebuff slowDebuffPrefab;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool hasImpacted;
    private float slowValue = 0.4f;
    private float slowTime = 2f;

    public void Configure(float impactRadius, float slow, float duration)
    {
        radius = impactRadius;
        slowValue = slow;
        slowTime = duration;
    }

    public override void Launch(Transform designatedTarget, GameObject designatedAttacker, int designatedDamage)
    {
        if (designatedTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        Launch(designatedTarget.position, designatedAttacker, designatedDamage);
    }

    public void Launch(Vector3 designatedPosition, GameObject designatedAttacker, int designatedDamage)
    {
        targetPosition = designatedPosition;
        startPosition = transform.position;
        attacker = designatedAttacker;
        damage = designatedDamage;
        elapsedTime = 0f;
        hasImpacted = false;
        isLaunched = true;
    }

    protected override void Update()
    {
        if (!isLaunched) return;

        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / Mathf.Max(0.01f, travelDuration));
        Vector3 position = Vector3.Lerp(startPosition, targetPosition, progress);
        position.y += arcCurve.Evaluate(progress) * arcHeight;
        transform.position = position;

        if (progress >= 1f)
        {
            transform.position = targetPosition;
            OnImpact();
        }
    }

    protected override void OnImpact()
    {
        if (hasImpacted) return;
        hasImpacted = true;
        isLaunched = false;

        HashSet<HealthController> damagedEnemies = new HashSet<HealthController>();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            HealthController hp = hit.GetComponentInParent<HealthController>();

            if (hp != null && hp.factionId == FactionId.Enemy && hp.CanBeTargeted() && damagedEnemies.Add(hp))
            {
                hp.GetDamaged(
                    attacker != null ? attacker : gameObject,
                    damage
                );

                if (hp != null && hp.CanBeTargeted())
                {
                    CreepController enemy = hp.GetComponentInParent<CreepController>();
                    if (enemy != null && slowDebuffPrefab != null)
                    {
                        SlowDebuff slow = enemy.statusEffectRoot.GetComponentInChildren<SlowDebuff>();
                        if (slow == null) slow = Instantiate(slowDebuffPrefab, enemy.statusEffectRoot.transform);
                        slow.ApplyOrRefresh(enemy, slowValue, slowTime);
                    }
                }
            }
        }

        Destroy(gameObject, 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(isLaunched ? targetPosition : transform.position, radius);
    }
}
