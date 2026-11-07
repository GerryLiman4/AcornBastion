using UnityEngine;

public class MageTowerController : TowerController
{
    [SerializeField] public Projectile projectilePrefab;

    protected override void Start()
    {
        base.Start();
        modelAnimator.PlayInFixedTime("Idle", 0, 0f);
    }

    public void LaunchProjectile()
    {
        if (isBuildMode || currentAttackTarget == null) return;

        Projectile projectile = Instantiate(
            projectilePrefab,
            transform.position,
            Quaternion.identity
        );

        IcePillar ice = projectile as IcePillar;
        if (ice != null && Definition != null)
            ice.Configure(Definition.splashRadius, Definition.slow, Definition.slowDuration);

        projectile.Launch(
            currentAttackTarget.transform,
            gameObject,
            damage
        );
    }
    public void OnAnimationEvent(string eventName)
    {
        if (isBuildMode) return;
        Invoke(eventName, 0f);
    }

    protected override void Attack()
    {
        if (isBuildMode) return;
        modelAnimator.PlayInFixedTime("Attack", 0, 0f);
        LaunchProjectile();
        attackTimer = attackCooldown;
    }
}
