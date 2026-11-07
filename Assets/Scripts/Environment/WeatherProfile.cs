using UnityEngine;
using UnityEngine.Rendering;

public enum WeatherPreset { Morning, Afternoon, Sunset, Night, Rain }

[CreateAssetMenu(menuName = "FUR/Weather Profile")]
public class WeatherProfile : ScriptableObject
{
    [Tooltip("URP post processing for this look: Color Adjustments, Vignette, and so on.")]
    public VolumeProfile volumeProfile;

    [Header("Rain")]
    public bool enableRain;
    [Min(0f)] public float rainRate = 320f;
    [Min(1f)] public float rainSpeed = 14f;
    public float wind = -2f;
}
