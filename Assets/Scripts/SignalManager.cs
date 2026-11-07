using System;

public static class SignalManager 
{
    public static event Action<HealthController, int> OnDamageDealt;

    public static void DamageDealt(HealthController target, int damage)
    {
        OnDamageDealt?.Invoke(target, damage);
    }

    public static event Action<CreepController, int> OnCreepGoldEarned;

    public static void CreepGoldEarned(CreepController creep, int amount)
    {
        OnCreepGoldEarned?.Invoke(creep, amount);
    }

    public static event Action OnPlayerBaseDestroyed;
    public static void PlayerBaseDestroyed()
    {
        OnPlayerBaseDestroyed?.Invoke();
    }

    public static event Action<MineralObject> OnMineralObjectDestroyed;

    public static void MineralObjectDestroyed(MineralObject destroyedObject)
    {
        OnMineralObjectDestroyed?.Invoke(destroyedObject);
    }
}
