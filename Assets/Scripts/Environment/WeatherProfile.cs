using UnityEngine;

public enum WeatherPreset { Morning, Afternoon, Sunset, Night, Rain }

[CreateAssetMenu(menuName = "FUR/Weather Profile")]
public class WeatherProfile : ScriptableObject
{
    [Range(-2f, 2f)] public float exposure;
    [Range(-0.5f, 0.5f)] public float contrast;
    [Range(0f, 2f)] public float saturation = 1f;
    [ColorUsage(false, true)] public Color colorFilter = Color.white;
    public Color shadowTint = Color.white;
    public Color highlightTint = Color.white;
    [Range(0f, 1f)] public float splitToning;
    public Color hazeColor = Color.gray;
    [Range(0f, 0.3f)] public float haze;
    [Range(0f, 0.6f)] public float vignette = 0.1f;
    public Color vignetteColor = new Color(.025f, .035f, .06f);

    [Header("Rain")]
    public bool enableRain;
    [Min(0f)] public float rainRate = 320f;
    [Min(1f)] public float rainSpeed = 14f;
    public float wind = -2f;
}
