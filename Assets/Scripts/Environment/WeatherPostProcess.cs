using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public class WeatherPostProcess : MonoBehaviour
{
    [SerializeField] private WeatherPreset preset;
    [SerializeField] private WeatherProfile[] profiles;
    [SerializeField] private Volume volume;
    [SerializeField] private ParticleSystem rain;
    [SerializeField, Min(.1f)] private float rainPlaneDistance = 10f;

    private Camera renderCamera;
    private WeatherPreset appliedPreset = (WeatherPreset)(-1);
    private bool wasRaining;

    public WeatherPreset ActivePreset => preset;

    private WeatherProfile CurrentProfile => profiles != null && (int)preset >= 0 && (int)preset < profiles.Length
        ? profiles[(int)preset] : null;

    private void OnEnable()
    {
        renderCamera = GetComponent<Camera>();
        appliedPreset = (WeatherPreset)(-1);
    }

    private void OnValidate() => appliedPreset = (WeatherPreset)(-1);

    public void SetPresetIndex(int index)
    {
        if (index < 0 || index > (int)WeatherPreset.Rain) return;
        preset = (WeatherPreset)index;
        ApplyWeather();
    }

    private void Update() => ApplyWeather();

    private void ApplyWeather()
    {
        ApplyVolumeProfile();
        UpdateRain();
    }

    private void ApplyVolumeProfile()
    {
        WeatherProfile profile = CurrentProfile;
        if (volume == null || profile == null) return;
        if (volume.sharedProfile != profile.volumeProfile) volume.sharedProfile = profile.volumeProfile;
    }

    private void UpdateRain()
    {
        if (rain == null || renderCamera == null) return;
        WeatherProfile profile = CurrentProfile;
        bool raining = profile != null && profile.enableRain;
        if (rain.gameObject.activeSelf != raining) rain.gameObject.SetActive(raining);
        if (raining)
        {
            rain.transform.position = renderCamera.ViewportToWorldPoint(new Vector3(.5f, 1.1f, rainPlaneDistance));
            float width = Vector3.Distance(renderCamera.ViewportToWorldPoint(new Vector3(0f, .5f, rainPlaneDistance)),
                renderCamera.ViewportToWorldPoint(new Vector3(1f, .5f, rainPlaneDistance)));
            var shape = rain.shape;
            shape.scale = new Vector3(width + 4f, .1f, .01f);
            var emission = rain.emission;
            emission.rateOverTime = profile.rainRate;
            var velocity = rain.velocityOverLifetime;
            velocity.x = profile.wind;
            velocity.y = -profile.rainSpeed;
            if (appliedPreset != preset || !wasRaining)
            {
                rain.Clear(true);
                if (Application.isPlaying) rain.Play(true);
                else
                {
                    rain.Simulate(.85f, true, true, true);
                    rain.Pause(true);
                }
            }
        }
        else if (wasRaining) rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        wasRaining = raining;
        appliedPreset = preset;
    }

    private void OnDisable()
    {
        if (rain != null) rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        wasRaining = false;
        appliedPreset = (WeatherPreset)(-1);
    }
}
