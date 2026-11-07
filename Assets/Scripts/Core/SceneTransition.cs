using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen pattern transition used by <see cref="SceneLoader"/>. Tiles grow across the screen in the order
/// given by <see cref="transitionOrder"/>, then shrink away in the same direction.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    [SerializeField] private Image pattern;
    [SerializeField] private Shader shader;

    [Header("Timing")]
    [Tooltip("Seconds for the pattern to cover the screen.")]
    [SerializeField, Min(0f)] private float coverDuration = .45f;
    [Tooltip("Seconds for the pattern to uncover the screen.")]
    [SerializeField, Min(0f)] private float uncoverDuration = .45f;
    [Tooltip("Easing over the cover and uncover animations.")]
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Look")]
    [SerializeField] private Color tileColor = new Color(.29f, .18f, .1f, 1f);
    [Tooltip("Shape of each tile. Uses alpha; keep a transparent border around the shape.")]
    [SerializeField] private Texture2D tilePattern;
    [Tooltip("Greyscale order of the wave: dark areas cover first.")]
    [SerializeField] private Texture2D transitionOrder;
    [Tooltip("Reverses the wave, so light areas cover first.")]
    [SerializeField] private bool invertOrder;
    [Tooltip("Tile size in pixels on a 1080p screen. Scales with screen height.")]
    [SerializeField, Min(1f)] private float tileSize = 72f;
    [Tooltip("How large a tile grows when fully covered. Above about 1.5, neighbouring tiles overlap and close the gaps.")]
    [SerializeField, Range(0f, 16f)] private float tileGrownScale = 2f;
    [Tooltip("How wide the moving edge of the wave is. Higher values make a softer, longer wave.")]
    [SerializeField, Range(.01f, 1f)] private float edgeWidth = .5f;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int WidthId = Shader.PropertyToID("_Width");
    private static readonly int TileTexId = Shader.PropertyToID("_TileTex");
    private static readonly int TileSizeId = Shader.PropertyToID("_TilePixelSize");
    private static readonly int GrownScaleId = Shader.PropertyToID("_TileGrownScale");
    private static readonly int OrderTexId = Shader.PropertyToID("_TransitionTex");
    private static readonly int InvertId = Shader.PropertyToID("_Invert");

    private Material material;
    private bool reversed;

    private void Awake()
    {
        if (pattern == null || shader == null) return;
        material = new Material(shader) { name = "SceneTransition (Instance)" };
        pattern.material = material;
        pattern.raycastTarget = true; // Blocks clicks while the screen is covered.
        pattern.enabled = false;
        ApplySettings();
    }

    // Lets you tune values in Play Mode and see them on the next transition.
    private void OnValidate() => ApplySettings();

    private void ApplySettings()
    {
        if (material == null) return;
        material.SetColor(ColorId, tileColor);
        material.SetFloat(WidthId, edgeWidth);
        material.SetTexture(TileTexId, tilePattern);
        material.SetFloat(TileSizeId, tileSize);
        material.SetFloat(GrownScaleId, tileGrownScale);
        material.SetTexture(OrderTexId, transitionOrder);
        material.SetFloat(InvertId, invertOrder != reversed ? 1f : 0f);
    }

    public IEnumerator Cover() => Animate(0f, 1f, false, coverDuration);

    // Tiles that appeared first disappear first, so the wave keeps moving the same way.
    public IEnumerator Uncover() => Animate(1f, 0f, true, uncoverDuration);

    private IEnumerator Animate(float from, float to, bool reverse, float duration)
    {
        if (material == null) yield break;
        reversed = reverse;
        ApplySettings();
        material.SetFloat(ProgressId, from);
        pattern.enabled = true;
        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            material.SetFloat(ProgressId, Mathf.LerpUnclamped(from, to, easing.Evaluate(time / duration)));
            yield return null;
        }
        material.SetFloat(ProgressId, to);
        pattern.enabled = to > 0f;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
