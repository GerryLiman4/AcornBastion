using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Exercises real builds, mining, combat, spells and scene transitions without granting resources.
// Run in Play Mode on Level.unity; reports stay outside Assets.
public static class CampaignPlaytest
{
    public static bool Running { get; private set; }
    public static string Status { get; private set; } = "Idle";
    private static string strategy;
    private static string prepared;
    private static LevelManager current;
    private static float previousScale;
    private static bool previousBackground;
    private static double started;
    private static int rotation;
    private static readonly List<string> log = new List<string>();

    public static void Begin(string buildStrategy)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        Stop(false, "Restarted");
        strategy = buildStrategy;
        prepared = null;
        current = null;
        rotation = 0;
        log.Clear();
        previousScale = Time.timeScale;
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        Time.timeScale = 12f;
        started = EditorApplication.timeSinceStartup;
        Running = true;
        Status = "Running " + strategy;
        GameManager.Instance.StartCampaign();
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!Running) return;
        if (!EditorApplication.isPlaying) { Stop(false, "Play Mode exited"); return; }
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (EditorApplication.timeSinceStartup - started > 300) { Stop(false, "Timed out"); return; }
            LevelManager level = UnityEngine.Object.FindFirstObjectByType<LevelManager>();
            if (level == null || level.Spells == null) return;
            if (level.isGameOver) { Stop(false, $"Base lost on L{level.LevelNumber} R{level.currentRound}"); return; }
            if (level.BaseHealth == null) return;
            if (current != level)
            {
                current = level;
                if (level.currentRound != 1 || level.currentGold != level.Definition.startingGold || level.Mana != 0
                    || level.BaseHealth.currentHp != level.Config.baseHealth)
                    throw new InvalidOperationException("Level reset failed.");
                log.Add($"L{level.LevelNumber}: fresh budget {level.currentGold}, mana 0, base {level.BaseHealth.currentHp}.");
            }
            if (level.isGameOver) { Stop(false, $"Base lost on L{level.LevelNumber} R{level.currentRound}"); return; }
            if (level.LevelComplete)
            {
                log.Add($"L{level.LevelNumber} CLEAR, base {level.BaseHealth.currentHp}, gold {level.currentGold}, mana {level.Mana}.");
                if (level.LevelNumber == level.Config.levels.Length) { Stop(true, "All five levels cleared"); return; }
                level.NextLevel();
                current = null;
                return;
            }
            if (level.currentState == GameStateId.Deploy)
            {
                string key = level.GetInstanceID() + ":" + level.currentRound;
                if (prepared == key) return;
                prepared = key;
                Prepare(level);
                log.Add($"L{level.LevelNumber} R{level.currentRound}: {level.Round.Total} enemies, "
                    + $"{level.MinerCount} miners, {UnityEngine.Object.FindObjectsByType<TowerController>(FindObjectsSortMode.None).Length} towers, "
                    + $"{level.currentGold} gold, {level.Mana} mana, {level.BaseHealth.currentHp} base.");
                Status = strategy + $": L{level.LevelNumber} R{level.currentRound}";
                level.StartWave();
                return;
            }
            if (level.currentState == GameStateId.Wave) CastSpells(level);
        }
        catch (Exception error) { Stop(false, error.ToString()); }
    }

    private static void Prepare(LevelManager level)
    {
        if (level.currentRound == 1 && strategy != "archer")
        {
            int miners = strategy == "max-miners" ? level.Definition.minerLimit : 1;
            for (int i = 0; i < miners; i++)
            {
                TilePiece tile = BestTile(level, level.Config.Tower(UnitId.FarmerTower));
                if (tile != null) level.TryPlaceTower(tile, UnitId.FarmerTower);
            }
        }
        foreach (MineralObject node in UnityEngine.Object.FindObjectsByType<MineralObject>(FindObjectsSortMode.None))
        {
            int count = node.tapCount;
            for (int i = 0; i < count; i++) if (!node.TryMine()) throw new InvalidOperationException("Mining failed in preparation.");
        }
        var choices = new List<TowerDefinition>();
        foreach (TowerDefinition tower in level.Config.towers)
            if (tower.id != (int)UnitId.FarmerTower && tower.unlockLevel <= level.LevelNumber
                && (strategy != "archer" || tower.id == (int)UnitId.ArcherTower)) choices.Add(tower);
        // Try the newly unlocked tower first, then maintain a mixed roster.
        if (strategy != "archer" && level.currentRound == 1) Buy(level, choices[choices.Count - 1]);
        for (int attempts = 0; attempts < 30; attempts++)
        {
            bool bought = false;
            for (int i = 0; i < choices.Count; i++)
            {
                TowerDefinition tower = choices[(rotation + i) % choices.Count];
                if (!Buy(level, tower)) continue;
                rotation = (rotation + i + 1) % choices.Count;
                bought = true;
                break;
            }
            if (!bought) break;
        }
    }

    private static bool Buy(LevelManager level, TowerDefinition tower)
    {
        if (!level.CanBuild((UnitId)tower.id)) return false;
        TilePiece tile = BestTile(level, tower);
        return tile != null && level.TryPlaceTower(tile, (UnitId)tower.id);
    }

    private static TilePiece BestTile(LevelManager level, TowerDefinition tower)
    {
        TilePiece best = null;
        float bestScore = float.NegativeInfinity;
        TowerController[] existing = UnityEngine.Object.FindObjectsByType<TowerController>(FindObjectsSortMode.None);
        foreach (GameObject obj in level.buildingPathList)
        {
            TilePiece tile = obj.GetComponent<TilePiece>();
            if (tile.isOccupied || !tile.canBeOccupied || tile.root == null) continue;
            Vector2 position = tile.root.transform.position;
            float score = 0;
            foreach (GameObject path in level.enemyPath)
            {
                float distance = Vector2.Distance(position, path.transform.position);
                if (tower.radius > 0 && distance <= tower.radius) score += 1 - distance / (tower.radius + 0.1f);
                if (tower.radius == 0) score += Mathf.Min(distance, 3);
            }
            int nearby = 0;
            foreach (TowerController built in existing)
                if (!built.isBuildMode && built.Definition != null && built.Definition.radius > 0
                    && Vector2.Distance(position, built.transform.position) < 2.5f) nearby++;
            score /= 1 + nearby * 0.65f;
            if (score <= bestScore) continue;
            bestScore = score;
            best = tile;
        }
        return best;
    }

    private static void CastSpells(LevelManager level)
    {
        if (level.creepControllerList.Count == 0) return;
        if (level.LevelNumber >= level.Config.burst.unlockLevel && level.Spells.BurstCooldown <= 0)
        {
            CreepController best = null;
            int bestCount = 0;
            foreach (CreepController target in level.creepControllerList)
            {
                int count = 0;
                foreach (CreepController other in level.creepControllerList)
                    if (Vector2.Distance(target.transform.position, other.transform.position) <= level.Config.burst.radius) count++;
                if (count > bestCount) { bestCount = count; best = target; }
            }
            if (bestCount >= 2 && level.Spells.TryCastBurst(best.transform.position)) return;
        }
        foreach (CreepController enemy in level.creepControllerList)
        {
            int index = Mathf.Clamp(enemy.nextDestinationIndex, 0, level.enemyPath.Length - 1);
            if (level.Spells.TryCastTrap(level.enemyPath[index].GetComponent<TilePiece>())) return;
        }
    }

    public static void Stop(bool passed, string reason)
    {
        if (!Running) return;
        Running = false;
        EditorApplication.update -= Tick;
        Time.timeScale = previousScale;
        Application.runInBackground = previousBackground;
        Status = (passed ? "PASS: " : "FAIL: ") + reason;
        log.Add(Status);
        Directory.CreateDirectory("Temp/CampaignValidation");
        File.WriteAllLines("Temp/CampaignValidation/Playtest-" + strategy + ".txt", log);
        Debug.Log("Campaign playtest " + strategy + ": " + Status);
    }
}
