using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public class WeatherPostProcess : MonoBehaviour
{
    [SerializeField] private WeatherPreset preset;
    [SerializeField] private WeatherProfile[] profiles;
    [SerializeField] private Shader colorGradeShader;
    [SerializeField] private ParticleSystem rain;
    [SerializeField, Min(.1f)] private float rainPlaneDistance = 10f;

    private Camera renderCamera;
    private Material material;
    private WeatherPreset appliedPreset = (WeatherPreset)(-1);
    private bool wasRaining;

    private static readonly int Exposure = Shader.PropertyToID("_Exposure");
    private static readonly int Contrast = Shader.PropertyToID("_Contrast");
    private static readonly int Saturation = Shader.PropertyToID("_Saturation");
    private static readonly int Filter = Shader.PropertyToID("_ColorFilter");
    private static readonly int ShadowTint = Shader.PropertyToID("_ShadowTint");
    private static readonly int HighlightTint = Shader.PropertyToID("_HighlightTint");
    private static readonly int SplitToning = Shader.PropertyToID("_SplitToning");
    private static readonly int HazeColor = Shader.PropertyToID("_HazeColor");
    private static readonly int Haze = Shader.PropertyToID("_Haze");
    private static readonly int Vignette = Shader.PropertyToID("_Vignette");
    private static readonly int VignetteColor = Shader.PropertyToID("_VignetteColor");
    private static readonly int Aspect = Shader.PropertyToID("_Aspect");

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
        UpdateRain();
    }

    private void Update() => UpdateRain();

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

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        WeatherProfile profile = CurrentProfile;
        if (profile == null || colorGradeShader == null || !colorGradeShader.isSupported)
        {
            Graphics.Blit(source, destination);
            return;
        }
        if (material == null)
            material = new Material(colorGradeShader) { hideFlags = HideFlags.HideAndDontSave };
        material.SetFloat(Exposure, profile.exposure);
        material.SetFloat(Contrast, profile.contrast);
        material.SetFloat(Saturation, profile.saturation);
        material.SetColor(Filter, profile.colorFilter);
        material.SetColor(ShadowTint, profile.shadowTint);
        material.SetColor(HighlightTint, profile.highlightTint);
        material.SetFloat(SplitToning, profile.splitToning);
        material.SetColor(HazeColor, profile.hazeColor);
        material.SetFloat(Haze, profile.haze);
        material.SetFloat(Vignette, profile.vignette);
        material.SetColor(VignetteColor, profile.vignetteColor);
        material.SetFloat(Aspect, (float)source.width / source.height);
        Graphics.Blit(source, destination, material);
    }

    private void OnDisable()
    {
        if (rain != null) rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (material != null)
        {
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
            material = null;
        }
        wasRaining = false;
        appliedPreset = (WeatherPreset)(-1);
    }
}
