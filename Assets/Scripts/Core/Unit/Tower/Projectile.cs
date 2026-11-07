
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float travelDuration = 0.8f;
    [SerializeField] protected float arcHeight = 2f;

    [Header("Trajectory")]
    [SerializeField]
    protected AnimationCurve arcCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f)
        );

    [Header("Visuals")]
    [SerializeField] protected Transform model;
    [SerializeField] protected float modelRotationOffset;

    protected Transform target;
    protected GameObject attacker;
    protected int damage;

    protected Vector3 startLocalPosition;
    protected float elapsedTime;
    protected bool isLaunched;

    public virtual void Launch(
        Transform designatedTarget,
        GameObject designatedAttacker,
        int designatedDamage)
    {
        if (designatedTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        target = designatedTarget;
        attacker = designatedAttacker;
        damage = designatedDamage;

        // Reparent without changing world position
        transform.SetParent(target, true);

        // Save starting position relative to enemy
        startLocalPosition = transform.localPosition;

        elapsedTime = 0f;
        isLaunched = true;
        UpdateModelRotation(0f);
    }

    protected virtual void Update()
    {
        if (!isLaunched) return;

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(
            elapsedTime / Mathf.Max(0.01f, travelDuration)
        );

        // Move toward enemy's local origin
        Vector3 newPosition = Vector3.Lerp(
            startLocalPosition,
            Vector3.zero,
            t
        );

        // Add arc offset
        newPosition.y += arcCurve.Evaluate(t) * arcHeight;

        transform.localPosition = newPosition;
        UpdateModelRotation(t);

        if (t >= 1f)
        {
            transform.localPosition = Vector3.zero;
            OnImpact();
        }
    }

    protected void UpdateModelRotation(float progress)
    {
        if (model == null) return;

        // Sample both sides so the visual follows custom curves, including their endpoints.
        float before = Mathf.Max(0f, progress - 0.001f);
        float after = Mathf.Min(1f, progress + 0.001f);
        Vector3 direction = -startLocalPosition;
        direction.y += (arcCurve.Evaluate(after) - arcCurve.Evaluate(before))
            / (after - before) * arcHeight;

        // The flight path is relative to the target; the model rotates in its own parent's space.
        Vector3 worldDirection = target.TransformVector(direction);
        Vector3 modelDirection = model.parent != null
            ? model.parent.InverseTransformVector(worldDirection)
            : worldDirection;

        if (modelDirection.x * modelDirection.x + modelDirection.y * modelDirection.y < 0.000001f) return;

        float angle = Mathf.Atan2(modelDirection.y, modelDirection.x) * Mathf.Rad2Deg;
        model.localRotation = Quaternion.Euler(0f, 0f, angle + modelRotationOffset);
    }

    protected virtual void OnImpact()
    {
        isLaunched = false;

        if (target != null)
        {
            HealthController hp =
                target.GetComponentInParent<HealthController>();

            if (hp != null && hp.CanBeTargeted())
            {
                hp.GetDamaged(
                    attacker != null ? attacker : gameObject,
                    damage
                );
            }
        }

        Destroy(gameObject);
    }
}
