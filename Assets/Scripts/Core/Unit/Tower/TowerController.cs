using UnityEngine;

public class TowerController : MonoBehaviour
{
    [SerializeField] public bool isBuildMode;
    [SerializeField] public bool isAggro;
    [SerializeField] public SpriteRenderer attackRangeIndicator;
    [SerializeField] public FactionId factionId = FactionId.Player;
    [SerializeField] public Animator modelAnimator;

    // tower info
    [SerializeField] public float attackRange = 2.0f;
    [SerializeField] public LayerMask hitboxLayer;
    public int damage = 5;
    public float attackCooldown = 1.5f;

    public float attackTimer;
    public HealthController currentAttackTarget;

    private SpriteRenderer[] modelSprites;
    private Color[] originalModelColors;
    private Collider2D[] towerColliders;
    private bool[] originalColliderStates;
    private bool appliedBuildMode;
    private bool suppressRangeHover;
    public TowerDefinition Definition { get; private set; }

    public void Configure(TowerDefinition definition)
    {
        Definition = definition;
        damage = definition.damage;
        attackCooldown = definition.cooldown;
        attackRange = definition.radius * 2f;
        GunnerTowerController gunner = this as GunnerTowerController;
        if (gunner != null)
        {
            gunner.magazineSize = definition.magazineSize;
            gunner.reloadDuration = definition.reloadDuration;
        }
        SetBuildMode(isBuildMode);
    }

    public virtual void BeginWave()
    {
        attackTimer = 0f;
        currentAttackTarget = null;
        isAggro = Definition != null && Definition.radius > 0f;
    }

    public void EndWave()
    {
        isAggro = false;
        currentAttackTarget = null;
        CancelInvoke();
    }

    protected virtual void Awake()
    {
        SetBuildMode(isBuildMode);
    }

    public void SetBuildMode(bool value)
    {
        if (modelSprites == null)
        {
            modelSprites = modelAnimator != null
                ? modelAnimator.GetComponentsInChildren<SpriteRenderer>(true)
                : new SpriteRenderer[0];
            originalModelColors = new Color[modelSprites.Length];
            for (int i = 0; i < modelSprites.Length; i++) originalModelColors[i] = modelSprites[i].color;
            towerColliders = GetComponentsInChildren<Collider2D>(true);
            originalColliderStates = new bool[towerColliders.Length];
            for (int i = 0; i < towerColliders.Length; i++) originalColliderStates[i] = towerColliders[i].enabled;
        }

        if (appliedBuildMode && !value) suppressRangeHover = true;
        isBuildMode = value;
        appliedBuildMode = value;
        for (int i = 0; i < modelSprites.Length; i++)
        {
            Color color = originalModelColors[i];
            if (isBuildMode) color *= new Color(.5f, .9f, 1f, .4f);
            modelSprites[i].color = color;
        }
        for (int i = 0; i < towerColliders.Length; i++)
            towerColliders[i].enabled = !isBuildMode && originalColliderStates[i];

        if (attackRangeIndicator != null)
        {
            attackRangeIndicator.transform.localScale = new Vector3(attackRange, attackRange, 1f);
            attackRangeIndicator.gameObject.SetActive(isBuildMode);
        }
        if (isBuildMode)
        {
            currentAttackTarget = null;
            CancelInvoke();
            if (modelAnimator != null && modelAnimator.isInitialized)
                modelAnimator.PlayInFixedTime("Idle", 0, 0f);
        }
    }

    protected virtual void Start()
    {
        attackTimer = attackCooldown;
        SetBuildMode(isBuildMode);
    }
    protected virtual void FixedUpdate()
    {
        if (isBuildMode || !isAggro) return;

        attackTimer -= Time.fixedDeltaTime;
        if (attackTimer > 0f) return;
        float radius = attackRange / 2f;
        if (currentAttackTarget != null && currentAttackTarget.CanBeTargeted()
            && currentAttackTarget.factionId != factionId
            && ((Vector2)(currentAttackTarget.transform.position - transform.position)).sqrMagnitude <= radius * radius)
        {
            Attack();
            return;
        }
        currentAttackTarget = null;
        foreach (Collider2D hit in GetTargetList())
        {
            HealthController candidate = hit.GetComponentInParent<HealthController>();
            if (candidate == null || candidate.factionId == factionId || !candidate.CanBeTargeted()) continue;
            currentAttackTarget = candidate;
            break;
        }
        if (currentAttackTarget != null) Attack();
    }

    protected virtual void LateUpdate()
    {
        if (appliedBuildMode != isBuildMode) SetBuildMode(isBuildMode);
        if (isBuildMode) return;
        if (currentAttackTarget == null || modelAnimator == null) return;

        Transform model = modelAnimator.transform;
        Vector3 modelScale = model.localScale;
        float targetOffsetX = currentAttackTarget.transform.position.x - transform.position.x;

        if (targetOffsetX > 0f) modelScale.x = Mathf.Abs(modelScale.x);
        else if (targetOffsetX < 0f) modelScale.x = -Mathf.Abs(modelScale.x);

        model.localScale = modelScale;
    }

    protected virtual Collider2D[] GetTargetList()
    {
        // check enemy
        float radius = attackRange / 2f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            radius,
            hitboxLayer
        );

        return hits;
    }

    protected virtual void Attack()
    {
        if (isBuildMode) return;
        currentAttackTarget.GetDamaged(gameObject, damage);
        attackTimer = attackCooldown;
    }

    protected void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange / 2f);
    }

    protected void OnMouseEnter()
    {
        if (!suppressRangeHover && attackRangeIndicator != null)
            attackRangeIndicator.gameObject.SetActive(true);
    }

    protected void OnMouseExit()
    {
        suppressRangeHover = false;
        if (!isBuildMode && attackRangeIndicator != null)
            attackRangeIndicator.gameObject.SetActive(false);
    }
}
