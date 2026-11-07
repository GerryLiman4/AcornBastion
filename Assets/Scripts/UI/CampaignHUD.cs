using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Builds the end-of-level panel on the scene's existing Canvas.
public sealed class CampaignHUD : MonoBehaviour
{
    private LevelManager owner;
    private TMP_FontAsset font;
    private TextMeshProUGUI outcomeTitle;
    private TextMeshProUGUI outcomeBody;
    private UnityEngine.UI.Button nextButton;
    private GameObject outcome;

    public void Initialize(LevelManager level)
    {
        owner = level;
        font = level.goldText.font;
        Canvas canvas = level.cardListUI.GetComponentInParent<Canvas>(true);
        outcome = Panel("CampaignOutcome", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 310));
        outcome.GetComponent<UnityEngine.UI.Image>().color = new Color(0.055f, 0.075f, 0.12f, 0.99f);
        outcomeTitle = Label(outcome.transform, "OutcomeTitle", new Vector2(0, 100), new Vector2(740, 60), 36);
        outcomeBody = Label(outcome.transform, "OutcomeBody", new Vector2(0, 25), new Vector2(710, 85), 23);
        nextButton = Button(outcome.transform, "NextLevel", new Vector2(0, -50), new Vector2(240, 50), owner.NextLevel, out TextMeshProUGUI next);
        next.text = "Next Level";
        Button(outcome.transform, "RetryLevel", new Vector2(-150, -112), new Vector2(230, 46), owner.Retry, out TextMeshProUGUI retry).name = "RetryLevel";
        retry.text = "Retry Level";
        Button(outcome.transform, "MainMenu", new Vector2(150, -112), new Vector2(230, 46), owner.GoToMainMenu, out TextMeshProUGUI menu).name = "MainMenu";
        menu.text = "Main Menu";
        outcome.SetActive(false);
        owner.Changed += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        if (owner == null) return;
        bool finished = owner.currentState == GameStateId.Finish;
        outcome.SetActive(finished);
        if (!finished) return;
        int health = owner.BaseHealth != null ? owner.BaseHealth.currentHp : 0;
        outcome.transform.SetAsLastSibling();
        outcomeTitle.text = owner.isGameOver ? "BASE LOST" : owner.LevelNumber == 5 ? "CAMPAIGN COMPLETE" : "LEVEL COMPLETE";
        outcomeTitle.color = owner.isGameOver ? new Color(1f, 0.45f, 0.35f) : new Color(0.4f, 1f, 0.75f);
        outcomeBody.text = owner.Notice + (owner.isGameOver ? "" : $"\nBase health remaining: {health}/{owner.Config.baseHealth}");
        nextButton.gameObject.SetActive(owner.LevelComplete && owner.LevelNumber < owner.Config.levels.Length);
    }

    private GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        obj.GetComponent<UnityEngine.UI.Image>().color = new Color(0.055f, 0.075f, 0.12f, 0.92f);
        return obj;
    }

    private TextMeshProUGUI Label(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private UnityEngine.UI.Button Button(Transform parent, string name, Vector2 position, Vector2 size,
        UnityAction action, out TextMeshProUGUI label)
    {
        GameObject obj = Panel(name, parent, new Vector2(0.5f, 0.5f), position, size);
        obj.GetComponent<UnityEngine.UI.Image>().color = new Color(0.14f, 0.3f, 0.36f, 1);
        var button = obj.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = obj.GetComponent<UnityEngine.UI.Image>();
        button.onClick.AddListener(action);
        label = Label(obj.transform, "Label", Vector2.zero, size - new Vector2(12, 4), 20);
        return button;
    }

    private void OnDestroy()
    {
        if (owner != null) owner.Changed -= Refresh;
        if (outcome != null) Destroy(outcome);
    }
}
