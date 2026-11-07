using System.Collections.Generic;
using UnityEngine;

public class CreepHitEffectSpawner : MonoBehaviour
{
    [SerializeField] private CreepHitEffect effectPrefab;
    [SerializeField, Min(0)] private int prewarmCount = 16;

    private readonly Queue<CreepHitEffect> available = new Queue<CreepHitEffect>();
    private readonly List<CreepHitEffect> effects = new List<CreepHitEffect>();

    private void Awake()
    {
        for (int i = 0; i < prewarmCount; i++) available.Enqueue(CreateEffect());
    }

    private void OnEnable()
    {
        SignalManager.OnDamageDealt += OnDamageDealt;
    }

    private void OnDisable()
    {
        SignalManager.OnDamageDealt -= OnDamageDealt;
        foreach (CreepHitEffect effect in effects)
        {
            if (effect != null) effect.Stop();
        }
    }

    private CreepHitEffect CreateEffect()
    {
        CreepHitEffect effect = Instantiate(effectPrefab, transform);
        effect.gameObject.SetActive(false);
        effect.Initialize(ReturnToPool);
        effects.Add(effect);
        return effect;
    }

    private void ReturnToPool(CreepHitEffect effect)
    {
        available.Enqueue(effect);
    }

    private void OnDamageDealt(HealthController target, int damage)
    {
        if (target == null || damage <= 0 || target.factionId != FactionId.Enemy) return;

        CreepController creep = target.GetComponentInParent<CreepController>();
        if (creep == null || creep.hpController != target) return;

        Vector3 position = target.hitbox != null ? target.hitbox.bounds.center : target.transform.position;
        CreepHitEffect effect = available.Count > 0 ? available.Dequeue() : CreateEffect();
        effect.Play(position);
    }
}
