
using UnityEngine;

public class ArcherTowerController : TowerController
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

        projectile.Launch(
            currentAttackTarget.transform,
            gameObject,
            damage
        );
        if (AudioManager.Instance != null) AudioManager.Instance.PlayArcherShot();
    }
    public void OnAnimationEvent(string eventName)
    {
        if (isBuildMode) return;
        Invoke(eventName,0f);
    }

    protected override void Attack()
    {
        if (isBuildMode) return;
        modelAnimator.PlayInFixedTime("Attack", 0, 0f);
        attackTimer = attackCooldown;
    }
}
