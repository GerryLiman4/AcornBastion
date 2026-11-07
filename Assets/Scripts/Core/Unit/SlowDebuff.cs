using UnityEngine;

public class SlowDebuff : StatusEffect
{
    public void ApplyOrRefresh(CreepController enemy, float strength, float seconds)
    {
        value = IsApplied ? Mathf.Max(value, strength) : strength;
        duration = seconds;
        if (!IsApplied) Apply(enemy);
        RefreshDuration(seconds);
        UpdateSpeedMultiplier();
    }
    private void Reset()
    {
        duration = 3f;
        value = 0.7f;
        effectName = "Slow";
    }

    public override void Apply(CreepController enemy)
    {
        base.Apply(enemy);
        if (IsApplied) UpdateSpeedMultiplier();
    }

    public override void Remove()
    {
        base.Remove();
        if (target != null) UpdateSpeedMultiplier();
    }

    private void UpdateSpeedMultiplier()
    {
        float strongestSlow = 0f;
        foreach (SlowDebuff slow in target.statusEffectRoot.GetComponentsInChildren<SlowDebuff>())
        {
            if (slow.IsApplied && slow.isActiveAndEnabled && slow.target == target)
                strongestSlow = Mathf.Max(strongestSlow, Mathf.Clamp01(slow.value));
        }
        target.speedMultiplier = 1f - strongestSlow;
    }
}
