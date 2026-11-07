using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class ElderShadowHealSkill : MonoBehaviour
{
    [SerializeField, Min(1)] private int healAmount = 2;
    [SerializeField, Min(0.01f)] private float healInterval = 3f;
    [SerializeField, Min(0f)] private float healRadius = 3f;

    [Header("Heal Pulse")]
    [SerializeField] private SpriteRenderer pulseSprite;
    [SerializeField] private Color pulseColor = new Color(0.3f, 1f, 0.5f, 0.55f);
    [SerializeField, Min(0.01f)] private float pulseDuration = 0.6f;

    private CreepController creep;
    private float healTimer;
    private Tween pulseTween;
    private readonly List<Collider2D> hits = new List<Collider2D>();
    private readonly HashSet<HealthController> healedTargets = new HashSet<HealthController>();

    public void Configure(int amount, float interval, float radius)
    {
        healAmount = amount;
        healInterval = Mathf.Max(0.01f, interval);
        healRadius = radius;
    }

    private void Awake()
    {
        creep = GetComponentInParent<CreepController>();
    }

    private void OnEnable()
    {
        healTimer = 0f;
        StopPulse();
    }

    private void Update()
    {
        if (creep == null || !creep.isActiveAndEnabled || creep.currentState == UnitStateId.Die ||
            creep.hpController == null || !creep.hpController.CanBeTargeted())
        {
            if (pulseTween != null) StopPulse();
            return;
        }

        healTimer += Time.deltaTime;
        if (healTimer < healInterval) return;

        healTimer = 0f;
        HealNearbyCreeps();
        PlayPulse();
    }

    private void PlayPulse()
    {
        StopPulse();
        if (pulseSprite == null || pulseSprite.sprite == null) return;

        Vector3 spriteSize = pulseSprite.sprite.bounds.size;
        Vector3 parentScale = pulseSprite.transform.parent.lossyScale;
        Vector3 fullScale = new Vector3(
            healRadius * 2f / Mathf.Max(0.001f, spriteSize.x * Mathf.Abs(parentScale.x)),
            healRadius * 2f / Mathf.Max(0.001f, spriteSize.y * Mathf.Abs(parentScale.y)), 1f);
        pulseSprite.transform.localScale = new Vector3(fullScale.x * 0.05f, fullScale.y * 0.05f, 1f);
        pulseSprite.color = pulseColor;
        pulseSprite.enabled = true;
        pulseTween = DOTween.Sequence()
            .Append(pulseSprite.transform.DOScale(fullScale, pulseDuration).SetEase(Ease.OutQuad))
            .Join(pulseSprite.DOFade(0f, pulseDuration).SetEase(Ease.InQuad))
            .SetTarget(pulseSprite.transform)
            .OnComplete(HidePulse)
            .OnKill(() => pulseTween = null);
    }

    private void HidePulse()
    {
        if (pulseSprite == null) return;
        pulseSprite.enabled = false;
        pulseSprite.transform.localScale = Vector3.zero;
    }

    private void StopPulse()
    {
        pulseTween?.Kill();
        pulseTween = null;
        HidePulse();
    }

    private void OnDisable()
    {
        StopPulse();
    }

    private void HealNearbyCreeps()
    {
        hits.Clear();
        healedTargets.Clear();
        ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
        Physics2D.OverlapCircle(transform.position, healRadius, filter, hits);

        foreach (Collider2D hit in hits)
        {
            CreepController ally = hit.GetComponentInParent<CreepController>();
            if (ally == null || ally == creep || !ally.isActiveAndEnabled ||
                ally.currentState == UnitStateId.Die) continue;

            HealthController health = ally.hpController;
            if (health == null || health.factionId != creep.hpController.factionId ||
                !health.CanBeTargeted() || !healedTargets.Add(health)) continue;

            health.Heal(healAmount);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}
