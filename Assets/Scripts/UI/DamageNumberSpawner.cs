using System.Collections.Generic;
using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private DamageNumberPopup popupPrefab;
    [SerializeField, Min(0)] private int prewarmCount = 24;
    [SerializeField, Min(0f)] private float heightOffset = 0.25f;
    [SerializeField] private Color damageColor = new Color(1f, 0.96f, 0.8f);
    [SerializeField] private Color playerDamageColor = new Color(1f, 0.42f, 0.35f);

    [Header("Gold Rewards")]
    [SerializeField] private Color goldColor = new Color(1f, 0.82f, 0.2f);
    [SerializeField, Min(0f)] private float goldHeightOffset = 1f;

    private readonly Queue<DamageNumberPopup> available = new Queue<DamageNumberPopup>();
    private readonly List<DamageNumberPopup> popups = new List<DamageNumberPopup>();

    private void Awake()
    {
        for (int i = 0; i < prewarmCount; i++) available.Enqueue(CreatePopup());
    }

    private void OnEnable()
    {
        SignalManager.OnDamageDealt += OnDamageDealt;
        SignalManager.OnCreepGoldEarned += OnCreepGoldEarned;
    }

    private void OnDisable()
    {
        SignalManager.OnDamageDealt -= OnDamageDealt;
        SignalManager.OnCreepGoldEarned -= OnCreepGoldEarned;
        foreach (DamageNumberPopup popup in popups)
        {
            if (popup != null) popup.Stop();
        }
    }

    private DamageNumberPopup CreatePopup()
    {
        DamageNumberPopup popup = Instantiate(popupPrefab, transform);
        popup.gameObject.SetActive(false);
        popup.Initialize(ReturnToPool);
        popups.Add(popup);
        return popup;
    }

    private void ReturnToPool(DamageNumberPopup popup)
    {
        available.Enqueue(popup);
    }

    private void OnDamageDealt(HealthController target, int damage)
    {
        if (target == null || damage <= 0) return;

        Vector3 position = target.transform.position;
        if (target.hitbox != null)
        {
            position.x = target.hitbox.bounds.center.x;
            position.y = target.hitbox.bounds.max.y;
        }
        position.y += heightOffset;
        position.x += Random.Range(-0.12f, 0.12f);

        DamageNumberPopup popup = available.Count > 0 ? available.Dequeue() : CreatePopup();
        popup.Show(position, damage, target.factionId == FactionId.Player ? playerDamageColor : damageColor);
    }

    private void OnCreepGoldEarned(CreepController creep, int amount)
    {
        if (creep == null || amount <= 0) return;

        Vector3 position = creep.transform.position;
        SpriteRenderer model = creep.modelAnimator != null
            ? creep.modelAnimator.GetComponent<SpriteRenderer>()
            : null;
        if (model != null)
        {
            position.x = model.bounds.center.x;
            position.y = model.bounds.max.y;
        }
        position.y += goldHeightOffset;

        DamageNumberPopup popup = available.Count > 0 ? available.Dequeue() : CreatePopup();
        popup.Show(position, "+" + amount, goldColor);
    }
}
