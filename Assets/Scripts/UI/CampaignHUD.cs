using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-of-level panel (CampaignOutcome prefab). Shown when the level reaches the Finish state:
/// base lost, level complete, or campaign complete.
/// </summary>
public sealed class CampaignHUD : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Object shown when the level finishes.")]
    [SerializeField] private GameObject outcomePanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [Tooltip("Hidden after a loss and on the last level.")]
    [SerializeField] private Button nextLevelButton;

    [Header("Titles")]
    [SerializeField] private string lostTitle = "BASE LOST";
    [SerializeField] private string levelCompleteTitle = "LEVEL COMPLETE";
    [SerializeField] private string campaignCompleteTitle = "CAMPAIGN COMPLETE";
    [SerializeField] private Color lostColor = new Color(1f, .45f, .35f);
    [SerializeField] private Color winColor = new Color(.4f, 1f, .75f);

    [Header("Body")]
    [Tooltip("Added under the level's message after a win. {0} is base health left, {1} is maximum base health.")]
    [SerializeField] private string baseHealthFormat = "Base health remaining: {0}/{1}";

    private LevelManager owner;

    public void Initialize(LevelManager level)
    {
        if (owner != null) owner.Changed -= Refresh;
        owner = level;
        owner.Changed += Refresh;
        Refresh();
    }

    // Button hooks: wired in the prefab's onClick lists.
    public void NextLevel() { if (owner != null) owner.NextLevel(); }
    public void RetryLevel() { if (owner != null) owner.Retry(); }
    public void MainMenu() { if (owner != null) owner.GoToMainMenu(); }

    private void Refresh()
    {
        // Enemies can still resolve while the scene unloads, after the panel is destroyed.
        if (owner == null || outcomePanel == null) return;
        bool finished = owner.currentState == GameStateId.Finish;
        outcomePanel.SetActive(finished);
        if (!finished) return;

        bool lastLevel = owner.LevelNumber >= owner.Config.levels.Length;
        if (titleText != null)
        {
            titleText.text = owner.isGameOver ? lostTitle : lastLevel ? campaignCompleteTitle : levelCompleteTitle;
            titleText.color = owner.isGameOver ? lostColor : winColor;
        }
        if (bodyText != null)
        {
            int health = owner.BaseHealth != null ? owner.BaseHealth.currentHp : 0;
            bodyText.text = owner.Notice + (owner.isGameOver ? "" : "\n" + string.Format(baseHealthFormat, health, owner.Config.baseHealth));
        }
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(owner.LevelComplete && !lastLevel);
    }

    private void OnDestroy()
    {
        if (owner != null) owner.Changed -= Refresh;
    }
}
