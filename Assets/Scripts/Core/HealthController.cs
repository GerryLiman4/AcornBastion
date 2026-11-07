using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthController : MonoBehaviour, IDamagable
{
    [SerializeField] public int currentHp;
    [SerializeField] public int maxHp = 5;
    [SerializeField] public Collider2D hitbox;
    [SerializeField] public Image healthBar;

    public FactionId factionId;

    public event Action Damaged;
    public event Action Died;

    public void Initialize(FactionId designatedFactionId)
    {
        factionId = designatedFactionId;
        currentHp = maxHp;
        if (hitbox != null) hitbox.enabled = true;
        UpdateBar();
    }

    public void GetDamaged(GameObject damager, int damage)
    {
        if (damage <= 0 || currentHp <= 0) return;
        int previousHp = currentHp;
        currentHp = (int)Mathf.Max((currentHp - damage) , 0f);
        UpdateBar();
        int appliedDamage = Mathf.Max(0, previousHp - currentHp);
        if (appliedDamage > 0) SignalManager.DamageDealt(this, appliedDamage);
        if (currentHp <= 0f)
        {
            if (hitbox != null) hitbox.enabled = false;
            Died?.Invoke();
            return;
        }
        Damaged?.Invoke();
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || !CanBeTargeted()) return;

        currentHp = Mathf.Min(currentHp + amount, maxHp);
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (healthBar != null) healthBar.fillAmount = maxHp > 0 ? (float)currentHp / maxHp : 0f;
    }

    private void OnDestroy()
    {
        Damaged = null;
        Died = null;
    }

    public FactionId GetFactionId()
    {
        return factionId;
    }

    public bool CanBeTargeted()
    {
        return currentHp > 0 && (hitbox == null || hitbox.enabled);
    }
}
