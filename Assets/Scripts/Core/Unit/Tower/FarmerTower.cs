using UnityEngine;

public class FarmerTower : TowerController
{
    private void OnEnable()
    {
        SignalManager.OnMineralObjectDestroyed += OnMineralObjectDestroyed;
    }

    private void OnDisable()
    {
        SignalManager.OnMineralObjectDestroyed -= OnMineralObjectDestroyed;
    }

    protected override void Start()
    {
        base.Start();
        modelAnimator.PlayInFixedTime("Idle", 0, 0f);
    }

    private void OnMineralObjectDestroyed(MineralObject mineral)
    {
        if (isBuildMode) return;
        modelAnimator.PlayInFixedTime("Attack", 0, 0f);
    }
    protected override void FixedUpdate()
    {

    }
}
