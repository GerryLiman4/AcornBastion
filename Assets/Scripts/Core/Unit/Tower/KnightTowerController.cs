using UnityEngine;

public class KnightTowerController : TowerController
{
    private const float TimerEpsilon = 0.0001f;

    [Header("Knight Slash")]
    [SerializeField] private SlashProjectile slashPrefab;

    private void Reset()
    {
        damage = 7;
        attackCooldown = 0.75f;
        attackRange = 2f;
    }

    protected override void Start()
    {
        base.Start();
        modelAnimator.PlayInFixedTime("Idle", 0, 0f);
    }

    protected override void FixedUpdate()
    {
        if (isBuildMode || !isAggro) return;

        attackTimer -= Time.fixedDeltaTime;
        if (attackTimer > TimerEpsilon) return;

        // Match the base tower's range indicator: attackRange is the diameter.
        float radius = attackRange / 2f;
        if (!IsEnemyInRange(currentAttackTarget, radius))
        {
            currentAttackTarget = null;
            float nearestDistance = float.MaxValue;
            foreach (Collider2D hit in GetTargetList())
            {
                HealthController candidate = hit.GetComponentInParent<HealthController>();
                if (!IsEnemyTargetable(candidate)) continue;

                float distance = ((Vector2)(candidate.transform.position - transform.position)).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    currentAttackTarget = candidate;
                }
            }
        }

        if (currentAttackTarget != null) Attack();
    }

    private bool IsEnemyInRange(HealthController candidate, float radius)
    {
        return IsEnemyTargetable(candidate) && ((Vector2)(candidate.transform.position - transform.position)).sqrMagnitude <= radius * radius + TimerEpsilon;
    }

    private bool IsEnemyTargetable(HealthController candidate)
    {
        return candidate != null && candidate.factionId != factionId && candidate.CanBeTargeted();
    }

    protected override void Attack()
    {
        if (isBuildMode || slashPrefab == null) return;

        attackTimer = Mathf.Max(0.01f, attackCooldown);
        modelAnimator.PlayInFixedTime("Attack", 0, 0f);
    }

    protected void LaunchProjectile()
    {
        float radius = attackRange / 2f;
        if (!IsEnemyTargetable(currentAttackTarget)) return;

        Vector2 direction = currentAttackTarget.transform.position - transform.position;
        if (direction.sqrMagnitude < TimerEpsilon)
            direction = modelAnimator.transform.localScale.x < 0f ? Vector2.left : Vector2.right;
        direction.Normalize();

        SlashProjectile slash = Instantiate(slashPrefab, transform.position, Quaternion.identity);
        slash.Launch(direction, GetImpactPoint(currentAttackTarget), gameObject, damage, factionId, hitboxLayer);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayKnightSlash();
    }

    // Aims at the middle of the enemy's sprite, since its transform can sit away from the body.
    private static Vector2 GetImpactPoint(HealthController target)
    {
        CreepController creep = target.GetComponentInParent<CreepController>();
        SpriteRenderer body = creep != null && creep.modelAnimator != null
            ? creep.modelAnimator.GetComponent<SpriteRenderer>()
            : null;
        return body != null ? body.bounds.center : target.transform.position;
    }

    public void OnAnimationEvent(string eventName)
    {
        if (isBuildMode) return;
        Invoke(eventName, 0f);
    }

}
