using UnityEngine;

public class StatusEffect : MonoBehaviour
{
    [SerializeField, Min(0f)] public float duration = 3f;
    [SerializeField] public float value;
    [SerializeField] public string effectName = "Status Effect";

    protected CreepController target;
    private float remainingDuration;

    public bool IsApplied { get; private set; }

    protected void RefreshDuration(float seconds)
    {
        remainingDuration = seconds;
    }

    public virtual void Apply(CreepController enemy)
    {
        if (IsApplied) return;

        target = enemy;
        if (target == null || duration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        remainingDuration = duration;
        IsApplied = true;
    }

    protected virtual void Start()
    {
        if (!IsApplied) Apply(GetComponentInParent<CreepController>());
    }

    protected virtual void Update()
    {
        if (!IsApplied) return;

        remainingDuration -= Time.deltaTime;
        if (target == null || target.currentState == UnitStateId.Die || remainingDuration <= 0f)
            Remove();
    }

    public virtual void Remove()
    {
        IsApplied = false;
        Destroy(gameObject);
    }

    protected virtual void OnDisable()
    {
        if (IsApplied) Remove();
    }
}
