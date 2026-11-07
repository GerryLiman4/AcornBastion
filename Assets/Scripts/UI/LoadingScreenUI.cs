using TMPro;
using UnityEngine;

/// <summary>Shows SceneLoader progress in the Loading scene.</summary>
public class LoadingScreenUI : MonoBehaviour
{
    [Tooltip("Stretched from the left edge of the bar to the current progress.")]
    [SerializeField] private RectTransform progressFill;
    [SerializeField] private TMP_Text label;
    [SerializeField] private string text = "Loading";
    [SerializeField, Min(.05f)] private float dotInterval = .3f;

    private void Update()
    {
        if (progressFill != null) progressFill.anchorMax = new Vector2(SceneLoader.Progress, progressFill.anchorMax.y);
        if (label != null) label.text = text + new string('.', (int)(Time.unscaledTime / dotInterval) % 4);
    }
}
