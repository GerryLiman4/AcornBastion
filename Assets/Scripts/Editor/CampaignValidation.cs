using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CampaignValidation
{
    [MenuItem("Tools/FUR/Validate Campaign Definitions")]
    public static void ValidateDefinitions()
    {
        CampaignConfig config = CampaignConfig.Load();
        Check(config.levels.Length == 5, "Campaign must have five levels.");
        int[,] expectedTotals = { { 6, 9, 12 }, { 9, 13, 17 }, { 12, 16, 20 }, { 15, 21, 27 }, { 18, 25, 33 } };
        int[] expectedMana = { 17, 19, 32, 34, 38 };
        int total = 0;
        int scenarios = 0;
        for (int level = 0; level < config.levels.Length; level++)
        {
            CampaignLevel definition = config.levels[level];
            Check(definition.rounds.Length == 3, "Each level must have three rounds.");
            int mana = definition.manaNodes * 3 * config.oreMana;
            foreach (TowerDefinition tower in config.towers)
            {
                Check(Resources.Load<TowerController>("Tower/" + (UnitId)tower.id) != null, "Missing tower prefab.");
                if (tower.unlockLevel == level + 1)
                    Check(definition.startingGold + definition.rounds[0].nodes * config.oreGold >= tower.cost,
                        "New tower cannot be purchased in its first preparation.");
            }
            for (int round = 0; round < 3; round++)
            {
                CampaignRound encounter = definition.rounds[round];
                Check(encounter.Total == expectedTotals[level, round], "Encounter total differs from the design.");
                Check(encounter.nodes >= definition.manaNodes, "Mana nodes must be part of the mining quota.");
                total += encounter.Total;
                mana += encounter.marked * config.markedMana;
                for (int seed = 0; seed < 100; seed++)
                {
                    List<CampaignSpawn> sequence = CampaignWavePlanner.Build(encounter, new System.Random(seed));
                    int grunts = 0, brutes = 0, elders = 0, marks = 0;
                    bool earlyMark = false;
                    Check(sequence.Count == encounter.Total, "Spawner must not lose or add enemies.");
                    for (int i = 0; i < sequence.Count; i++)
                    {
                        CampaignSpawn spawn = sequence[i];
                        if (i < 3) Check(spawn.Id == UnitId.ShadowGrunt, "Opening enemies must be Grunts.");
                        switch (spawn.Id)
                        {
                            case UnitId.ShadowGrunt: grunts++; break;
                            case UnitId.ShadowBrute: brutes++; break;
                            case UnitId.ShadowElder:
                                elders++;
                                Check(i > 0 && sequence[i - 1].Id == UnitId.ShadowBrute, "Elder must follow a Brute.");
                                break;
                        }
                        if (!spawn.Marked) continue;
                        Check(spawn.Id == UnitId.ShadowGrunt, "Only Grunts can be marked.");
                        marks++;
                        earlyMark |= i < sequence.Count / 2;
                    }
                    Check(grunts == encounter.grunts && brutes == encounter.brutes && elders == encounter.elders,
                        "Spawner composition differs from the authored encounter.");
                    Check(marks == encounter.marked && earlyMark, "Marked quota or early-wave guarantee failed.");
                    scenarios++;
                }
            }
            Check(mana == expectedMana[level], "Mana income differs from the campaign budget.");
        }
        Check(total == 253, "Campaign total must be 253 enemies.");
        foreach (EnemyDefinition enemy in config.enemies)
            Check(Resources.Load<CreepController>("Enemy/" + (UnitId)enemy.id) != null && enemy.health.Length == 5,
                "Missing enemy prefab or level health data.");
        int openingBudget = config.levels[0].startingGold - config.Tower(UnitId.FarmerTower).cost
            + config.levels[0].rounds[0].nodes * (config.oreGold + config.minerBonus);
        Check(openingBudget - 2 * config.Tower(UnitId.ArcherTower).cost == 30, "Tutorial opening must leave 30 gold.");
        Check(config.levels[0].startingGold + 4 * config.oreGold >= 2 * config.Tower(UnitId.ArcherTower).cost,
            "Tutorial must support an opening without a Miner.");
        string message = $"Campaign definitions PASS: {scenarios} seeded wave scenarios; 5 levels, 15 rounds, 253 enemies; prefab, mana and opening budgets verified.";
        Directory.CreateDirectory("Temp/CampaignValidation");
        File.WriteAllText("Temp/CampaignValidation/Definitions.txt", message);
        Debug.Log(message);
    }

    public static void BuildWindows()
    {
        for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; i++)
            Check(!UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i).isDirty,
                "Save or review unsaved scenes before building; validation never answers scene-save dialogs automatically.");
        ValidateDefinitions();
        Directory.CreateDirectory("Temp/CampaignValidation/Player");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Level.unity", "Assets/Scenes/Level 1.unity",
                "Assets/Scenes/Level 2.unity", "Assets/Scenes/Level 3.unity", "Assets/Scenes/Level 4.unity",
            },
            locationPathName = "Temp/CampaignValidation/Player/FUR.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        File.WriteAllText("Temp/CampaignValidation/Build.txt", report.summary.result + "; errors="
            + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
        Check(report.summary.result == BuildResult.Succeeded, "Windows build failed.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
