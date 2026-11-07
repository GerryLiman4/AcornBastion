using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WeatherPreviewControls : MonoBehaviour
{
    [SerializeField] private WeatherPostProcess weather;
    [SerializeField] private Button[] buttons;
    [SerializeField] private Color normalColor = new Color(.15f, .2f, .25f, .95f);
    [SerializeField] private Color selectedColor = new Color(.48f, .64f, .25f, 1f);
    private UnityAction[] handlers;
    private WeatherPreset displayedPreset = (WeatherPreset)(-1);

    private void OnEnable()
    {
        if (weather == null || buttons == null) return;
        handlers = new UnityAction[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            handlers[i] = () => { weather.SetPresetIndex(index); RefreshSelection(); };
            if (buttons[i] != null) buttons[i].onClick.AddListener(handlers[i]);
        }
        RefreshSelection();
    }

    private void LateUpdate()
    {
        if (weather != null && weather.ActivePreset != displayedPreset) RefreshSelection();
    }

    private void RefreshSelection()
    {
        displayedPreset = weather.ActivePreset;
        for (int i = 0; i < buttons.Length; i++)
            if (buttons[i] != null && buttons[i].image != null)
                buttons[i].image.color = i == (int)displayedPreset ? selectedColor : normalColor;
    }

    private void OnDisable()
    {
        if (handlers == null || buttons == null) return;
        for (int i = 0; i < buttons.Length && i < handlers.Length; i++)
            if (buttons[i] != null) buttons[i].onClick.RemoveListener(handlers[i]);
        handlers = null;
    }
}
