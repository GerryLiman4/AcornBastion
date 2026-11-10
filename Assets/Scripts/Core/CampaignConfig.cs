using System;
using UnityEngine;

// Authored in Resources/Campaign/JamCampaign.json; runtime state never modifies this data.
[Serializable]
public sealed class CampaignConfig
{
    public int baseHealth;
    public int manaCap;
    public int oreGold;
    public int oreClicks;
    public int oreMana;
    public int minerBonus;
    public int markedMana;
    public int clearBonus;
    public TowerDefinition[] towers;
    public EnemyDefinition[] enemies;
    public SpellDefinition trap;
    public SpellDefinition burst;
    public CampaignLevel[] levels;

    public static CampaignConfig Load()
    {
        TextAsset asset = Resources.Load<TextAsset>("Campaign/JamCampaign");
        if (asset == null) throw new InvalidOperationException("Missing Resources/Campaign/JamCampaign.json.");
        return JsonUtility.FromJson<CampaignConfig>(asset.text);
    }

    public TowerDefinition Tower(UnitId id) => Array.Find(towers, item => item.id == (int)id);
    public EnemyDefinition Enemy(UnitId id) => Array.Find(enemies, item => item.id == (int)id);
    public CampaignLevel Level(int number) => levels[Mathf.Clamp(number - 1, 0, levels.Length - 1)];
}

[Serializable]
public sealed class TowerDefinition
{
    public int id;
    public string name;
    public int unlockLevel;
    public int cost;
    public int damage;
    public float cooldown;
    public float radius;
    public float splashRadius;
    public float slow;
    public float slowDuration;
    public int magazineSize;
    public float reloadDuration;
}

[Serializable]
public sealed class EnemyDefinition
{
    public int id;
    public int[] health;
    public float speed;
    public int bounty;
    public int baseDamage;
    public int healAmount;
    public float healInterval;
    public float healRadius;
}

[Serializable]
public sealed class SpellDefinition
{
    public int unlockLevel;
    public int cost;
    public float cooldown;
    public int damage;
    public float duration;
    public float tickInterval;
    public float radius;
    public int maxActive;
}

[Serializable]
public sealed class CampaignLevel
{
    public string name;
    public string lesson;
    public int startingGold;
    public int minerLimit;
    public int manaNodes;
    // Multiplies every enemy speed on this level; 0 or missing means 1.
    public float enemySpeed;
    public CampaignRound[] rounds;
}

[Serializable]
public sealed class CampaignRound
{
    public int grunts;
    public int brutes;
    public int elders;
    public int marked;
    public int nodes;
    public float interval;
    public int Total => grunts + brutes + elders;
    public string Preview => $"Grunts {grunts}   Brutes {brutes}   Elders {elders}   |   Mana marks {marked}";
}
