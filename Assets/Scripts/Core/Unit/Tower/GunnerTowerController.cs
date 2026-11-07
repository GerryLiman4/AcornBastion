using UnityEngine;

public class GunnerTowerController : TowerController
{
    private const float TimerEpsilon = 0.0001f;

    [Header("Gunner")]
    [SerializeField] public Projectile projectilePrefab;
    [SerializeField, Min(1)] public int magazineSize = 20;
    [SerializeField, Min(0.01f)] public float reloadDuration = 2f;

    public int CurrentAmmo { get; private set; }
    public bool IsReloading { get; private set; }

    private float reloadTimer;
    private void OnDisable()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.StopMachineGun(this);
    }

    public override void BeginWave()
    {
        base.BeginWave();
        CurrentAmmo = Mathf.Max(1, magazineSize);
        IsReloading = false;
        reloadTimer = 0f;
    }

    private void Reset()
    {
        damage = 1;
        attackCooldown = 0.1f;
    }

    protected override void Start()
    {
        base.Start();
        modelAnimator.PlayInFixedTime("Idle", 0, 0f);
        CurrentAmmo = Mathf.Max(1, magazineSize);
    }

    protected override void FixedUpdate()
    {
        if (isBuildMode || !isAggro) return;

        if (IsReloading)
        {
            reloadTimer -= Time.fixedDeltaTime;
            if (reloadTimer <= TimerEpsilon)
            {
                CurrentAmmo = Mathf.Max(1, magazineSize);
                IsReloading = false;
                attackTimer = 0f;
            }
            return;
        }

        attackTimer -= Time.fixedDeltaTime;
        if (attackTimer > TimerEpsilon) return;

        float radius = attackRange / 2f;
        if (currentAttackTarget == null || !currentAttackTarget.CanBeTargeted()
            || currentAttackTarget.factionId == factionId
            || ((Vector2)(currentAttackTarget.transform.position - transform.position)).sqrMagnitude > radius * radius)
        {
            currentAttackTarget = null;
            foreach (Collider2D hit in GetTargetList())
            {
                HealthController candidate = hit.GetComponent<HealthController>();
                if (candidate != null && candidate.factionId != factionId && candidate.CanBeTargeted())
                {
                    currentAttackTarget = candidate;
                    break;
                }
            }
        }

        if (currentAttackTarget != null) Attack();
    }

    protected override void Attack()
    {
        if (isBuildMode || IsReloading || CurrentAmmo <= 0 || currentAttackTarget == null) return;

        modelAnimator.PlayInFixedTime("Attack", 0, 0f);
        Projectile projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectile.Launch(currentAttackTarget.transform, gameObject, damage);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayMachineGun(this);

        CurrentAmmo--;
        attackTimer = Mathf.Max(0.01f, attackCooldown);
        if (CurrentAmmo == 0)
        {
            IsReloading = true;
            reloadTimer = Mathf.Max(0.01f, reloadDuration);
        }
    }
}
