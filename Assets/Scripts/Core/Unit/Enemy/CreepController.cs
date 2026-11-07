using System;
using UnityEngine;

public enum EnemyResolution { CombatKill, BaseArrival, Cleanup }

public class CreepController : MonoBehaviour
{
    [SerializeField] public FactionId factionId;
    [SerializeField] public HealthController hpController;
    [SerializeField] public AttackArea attackArea;
    [SerializeField] public UnitStateId currentState;
    [SerializeField] public Animator modelAnimator;
    [SerializeField] public GameObject[] path;
    [SerializeField] public GameObject statusEffectRoot;
    [SerializeField] private GameObject manaIndicatorPrefab;

    [Header("Death Launch")]
    [SerializeField] private SpriteRenderer shadow;
    [SerializeField, Min(0.1f)] private float deathFlightDuration = 0.9f;
    [SerializeField, Min(0f)] private float deathSpins = 2f;

    public int currentPathIndex = -1;
    public int nextDestinationIndex = -1;

    // unit info
    public float moveSpeed;
    public float speedMultiplier = 1f;
    public int damage = 1;
    public UnitId Id { get; private set; }
    public int Bounty { get; private set; }
    public bool IsMarked { get; private set; }
    public bool IsResolved { get; private set; }
    public event Action<CreepController, EnemyResolution> Resolved;

    public void Configure(UnitId id, EnemyDefinition definition, int level, bool marked)
    {
        Id = id;
        Bounty = definition.bounty;
        damage = definition.baseDamage;
        moveSpeed = definition.speed;
        hpController.maxHp = definition.health[Mathf.Clamp(level - 1, 0, definition.health.Length - 1)];
        IsMarked = marked;
        if (marked) Instantiate(manaIndicatorPrefab, transform.position + Vector3.up * 1.1f, Quaternion.identity, transform);
        ElderShadowHealSkill heal = GetComponentInChildren<ElderShadowHealSkill>();
        if (heal != null) heal.Configure(definition.healAmount, definition.healInterval, definition.healRadius);
    }

    public void ReachBase() => Resolve(EnemyResolution.BaseArrival);
    public void Cleanup() => Resolve(EnemyResolution.Cleanup);

    private void Resolve(EnemyResolution reason)
    {
        if (IsResolved) return;
        IsResolved = true;
        currentState = UnitStateId.Die;
        // Notify while the model still exists, so kill feedback has a valid origin.
        Resolved?.Invoke(this, reason);
        if (reason == EnemyResolution.CombatKill) Die();
        else
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
    public void SwitchState(UnitStateId designatedStateId, bool canOverride = false)
    {
        if (currentState == designatedStateId && !canOverride) return;

        OnStateExited();
        currentState = designatedStateId;
        OnStateEntered();
    }

    public void OnStateEntered()
    {
        
        switch (currentState)
        {
            case UnitStateId.None:
                break;
            case UnitStateId.Idle:
                break;
            case UnitStateId.Walk:
                modelAnimator.PlayInFixedTime("Walk", 0, 0f);
                break;
            case UnitStateId.Die:
                Die();
                break;
            case UnitStateId.Custom:
                break;
        }
    }

    public void OnStateExited()
    {
        switch (currentState)
        {
            case UnitStateId.None:
                break;
            case UnitStateId.Idle:
                break;
            case UnitStateId.Walk:
                break;
            case UnitStateId.Die:
                break;
            case UnitStateId.Custom:
                break;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hpController.Damaged += OnDamaged;
        hpController.Died += OnDied;

        hpController.Initialize(factionId);
        attackArea.InitializeAttackArea(factionId, damage);
        //
        SetNextDestination(0);
        SwitchState(UnitStateId.Walk);
    }

    private void OnDied()
    {
        Resolve(EnemyResolution.CombatKill);
    }

    private void OnDamaged()
    {
        // flashing
    }

    private void OnDestroy()
    {
        if (!IsResolved) Resolve(EnemyResolution.Cleanup);
        Resolved = null;
        if (hpController != null)
        {
            hpController.Damaged -= OnDamaged;
            hpController.Died -= OnDied;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsResolved) return;
        if (path == null || path.Length == 0) { Cleanup(); return; }
        switch (currentState)
        {
            case UnitStateId.None:
                break;
            case UnitStateId.Idle:

                break;
            case UnitStateId.Walk:
                MoveToNextDestination();
               
                if(Vector2.Distance(transform.position, path[nextDestinationIndex].transform.position) <= 0.01f )
                {
                    currentPathIndex = nextDestinationIndex;
                    if (nextDestinationIndex + 1 >= path.Length)
                    {
                        ReachBase();
                        return;
                    }
                    SetNextDestination(nextDestinationIndex + 1);
                }
                break;
            case UnitStateId.Die:
                hpController.gameObject.SetActive(false);
                break;
            case UnitStateId.Custom:
                break;
        }
    }

    public void SetNextDestination(int index)
    {
        nextDestinationIndex = index;
    }
    public void MoveToNextDestination()
    {
        Vector2 currentPosition = transform.position;
        Vector2 nextPosition = Vector2.MoveTowards(currentPosition, path[nextDestinationIndex].transform.position, moveSpeed * speedMultiplier * Time.deltaTime);
        float movementX = nextPosition.x - currentPosition.x;

        if (movementX != 0f)
        {
            Vector3 modelScale = modelAnimator.transform.localScale;
            modelScale.x = movementX > 0f ? 1f : -1f;
            modelAnimator.transform.localScale = modelScale;
        }

        transform.position = nextPosition;
    }

    private void Die()
    {
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        if (hpController != null) hpController.gameObject.SetActive(false);
        if (statusEffectRoot != null) statusEffectRoot.SetActive(false);
        if (modelAnimator != null)
            CreepDeathFlight.Play(modelAnimator, shadow, deathFlightDuration, deathSpins);
        Destroy(gameObject);
    }


}
