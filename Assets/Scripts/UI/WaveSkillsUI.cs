using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Wave-time spell bar. Buttons connect in code so a fresh prefab instance needs no scene wiring.
public sealed class WaveSkillsUI : MonoBehaviour
{
    [SerializeField] private Button trapButton;
    [SerializeField] private TextMeshProUGUI trapLabel;
    [SerializeField] private Button burstButton;
    [SerializeField] private TextMeshProUGUI burstLabel;

    private LevelManager owner;
    private int lastTrapSecond = -1;
    private int lastBurstSecond = -1;

    public void Initialize(LevelManager level)
    {
        owner = level;
        trapButton.onClick.AddListener(() => OnSkillClicked(WaveSpell.Trap));
        burstButton.onClick.AddListener(() => OnSkillClicked(WaveSpell.Burst));
        owner.Changed += Refresh;
        Refresh();
    }

    private void OnSkillClicked(WaveSpell spell)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectCard();
        owner.Spells.Select(spell);
    }

    private void Refresh()
    {
        if (owner == null || owner.Spells == null) return;
        int health = owner.BaseHealth != null ? owner.BaseHealth.currentHp : 0;
        UpdateSpellButtons();
    }

    private void Update()
    {
        if (owner == null || owner.Spells == null) return;
        int trap = Mathf.CeilToInt(owner.Spells.TrapCooldown);
        int burst = Mathf.CeilToInt(owner.Spells.BurstCooldown);
        if (trap != lastTrapSecond || burst != lastBurstSecond) UpdateSpellButtons();
    }

    private void UpdateSpellButtons()
    {
        lastTrapSecond = Mathf.CeilToInt(owner.Spells.TrapCooldown);
        lastBurstSecond = Mathf.CeilToInt(owner.Spells.BurstCooldown);
        bool wave = owner.currentState == GameStateId.Wave && !owner.isGameOver;
        trapLabel.text = SpellText("Damage Trap", owner.Config.trap.cost, lastTrapSecond, owner.Spells.Selected == WaveSpell.Trap);
        burstLabel.text = owner.LevelNumber < owner.Config.burst.unlockLevel ? "Area Burst - Unlocks L3"
            : SpellText("Area Burst", owner.Config.burst.cost, lastBurstSecond, owner.Spells.Selected == WaveSpell.Burst);
        trapButton.interactable = wave && owner.Mana >= owner.Config.trap.cost && lastTrapSecond == 0;
        burstButton.interactable = wave && owner.LevelNumber >= owner.Config.burst.unlockLevel
            && owner.Mana >= owner.Config.burst.cost && lastBurstSecond == 0;
    }

    private static string SpellText(string name, int cost, int cooldown, bool selected)
        => $"{(selected ? "> " : "")}{name}\n{cost} mana{(cooldown > 0 ? $"  |  {cooldown}s" : "")}";

    private void OnDestroy()
    {
        if (owner != null) owner.Changed -= Refresh;
    }
}
