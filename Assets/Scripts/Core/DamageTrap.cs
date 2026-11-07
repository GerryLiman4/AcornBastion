using System.Collections.Generic;
using UnityEngine;

public sealed class DamageTrap : MonoBehaviour
{
    public TilePiece Tile { get; private set; }
    private LevelManager owner;
    private SpellDefinition definition;
    private float expiresAt;
    private Bounds footprint;
    private readonly Dictionary<HealthController, float> nextHit = new Dictionary<HealthController, float>();

    public void Initialize(LevelManager level, TilePiece tile, Bounds bounds)
    {
        owner = level;
        definition = level.Config.trap;
        Tile = tile;
        footprint = bounds;
        expiresAt = Time.time + definition.duration;
    }

    private void Update()
    {
        if (owner == null || owner.currentState != GameStateId.Wave || Time.time >= expiresAt)
        { gameObject.SetActive(false); Destroy(gameObject); return; }
        for (int i = owner.creepControllerList.Count - 1; i >= 0; i--)
        {
            CreepController enemy = owner.creepControllerList[i];
            if (enemy == null || enemy.IsResolved || !enemy.hpController.CanBeTargeted()) continue;
            Vector3 position = enemy.transform.position;
            if (position.x < footprint.min.x || position.x > footprint.max.x
                || position.y < footprint.min.y || position.y > footprint.max.y) continue;
            if (nextHit.TryGetValue(enemy.hpController, out float due) && Time.time < due) continue;
            // Retain this timestamp on exit, so re-entry cannot manufacture extra ticks.
            nextHit[enemy.hpController] = Time.time + definition.tickInterval;
            enemy.hpController.GetDamaged(gameObject, definition.damage);
            if (owner.currentState != GameStateId.Wave) break;
        }
    }
}
