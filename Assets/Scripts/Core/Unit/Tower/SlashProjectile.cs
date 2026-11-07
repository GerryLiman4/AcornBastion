using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SlashProjectile : MonoBehaviour
{
    [Header("Damage Box (local Y is forward)")]
    [SerializeField] private Vector2 boxSize = new Vector2(2.5f, 1.25f);
    [SerializeField] private Vector2 boxOffset = new Vector2(0f, 0.625f);

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer model;
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.01f)] private float duration = 0.18f;
    [Tooltip("Degrees added to the model. The crescent's tips point along +Y, so 180 leads with its outer curve.")]
    [SerializeField] private float modelRotationOffset = 180f;

    private bool isLaunched;
    private float elapsed;
    private Color originalColor;
    private Vector3 originalScale;
    private readonly List<Collider2D> hits = new List<Collider2D>();
    private readonly HashSet<HealthController> hitEnemies = new HashSet<HealthController>();

    private void Awake()
    {
        if (model == null) return;
        originalColor = model.color;
        originalScale = model.transform.localScale;
    }
    private void Start()
    {
        animator.PlayInFixedTime("Appear");
    }

    public void Launch(Vector2 direction, Vector2 impactPoint, GameObject attacker, int damage, FactionId faction, LayerMask hitboxLayer)
    {
        if (isLaunched) return;
        isLaunched = true;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        PlaceModelOn(impactPoint);

        Vector3 scale = transform.lossyScale;
        Vector2 size = new Vector2(boxSize.x * Mathf.Abs(scale.x), boxSize.y * Mathf.Abs(scale.y));
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(hitboxLayer);
        Physics2D.OverlapBox(transform.TransformPoint(boxOffset), size, transform.eulerAngles.z, filter, hits);
        foreach (Collider2D hit in hits)
        {
            HealthController health = hit.GetComponentInParent<HealthController>();
            if (health == null || health.factionId == faction || health.factionId == FactionId.Neutral ||
                !health.CanBeTargeted() || !hitEnemies.Add(health)) continue;
            health.GetDamaged(attacker != null ? attacker : gameObject, damage);
        }
    }

    // Only the visual moves: the damage box above stays anchored to the attacker.
    private void PlaceModelOn(Vector2 impactPoint)
    {
        if (model == null) return;
        model.transform.localRotation = Quaternion.Euler(0f, 0f, modelRotationOffset);
        model.transform.position += (Vector3)(impactPoint - (Vector2)model.bounds.center);
    }

    private void Update()
    {
        if (!isLaunched) return;
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
        if (model != null)
        {
            model.transform.localScale = originalScale * Mathf.Lerp(0.85f, 1f, progress);
            Color color = originalColor;
            color.a *= 1f - progress;
            model.color = color;
        }
        if (progress >= 1f) Destroy(gameObject);
    }

    //private void OnDrawGizmosSelected()
    //{
    //    Matrix4x4 previous = Gizmos.matrix;
    //    Gizmos.matrix = transform.localToWorldMatrix;
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireCube(boxOffset, boxSize);
    //    Gizmos.matrix = previous;
    //}
}
