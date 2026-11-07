using System.Collections.Generic;
using UnityEngine;

// Add to a SpriteRenderer. The shader draws only the outer edge on a temporary padded quad.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public class SpriteOutline : MonoBehaviour
{
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField, Range(0f, 8f)] private float outlineWidth = 2f;

    private struct SpriteData
    {
        public Vector4 metrics;
        public Vector2 uvOrigin, uvPixelX, uvPixelY;
        public Texture2D texture, alphaTexture;
        public float pixelsPerUnit;
    }

    private static readonly Dictionary<Sprite, SpriteData> SpriteCache = new Dictionary<Sprite, SpriteData>();
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int AlphaTex = Shader.PropertyToID("_AlphaTex");
    private static readonly int ExternalAlpha = Shader.PropertyToID("_EnableExternalAlpha");
    private static readonly int Metrics = Shader.PropertyToID("_SpriteMetrics");
    private static readonly int UVOrigin = Shader.PropertyToID("_UVOrigin");
    private static readonly int UVPixelX = Shader.PropertyToID("_UVPixelX");
    private static readonly int UVPixelY = Shader.PropertyToID("_UVPixelY");
    private static readonly int PixelsPerUnit = Shader.PropertyToID("_PixelsPerUnit");
    private static readonly int Flip = Shader.PropertyToID("_SpriteFlip");
    private static readonly int Opacity = Shader.PropertyToID("_SpriteOpacity");
    private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");

    private SpriteRenderer source;
    private MeshRenderer outlineRenderer;
    private Mesh quad;
    private MaterialPropertyBlock properties;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => SpriteCache.Clear();

    #if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void WatchSpriteReimports()
    {
        UnityEditor.EditorApplication.projectChanged -= ResetCache;
        UnityEditor.EditorApplication.projectChanged += ResetCache;
    }
    #endif

    private void OnEnable()
    {
        source = GetComponent<SpriteRenderer>();
        properties = new MaterialPropertyBlock();
        var child = new GameObject("Sprite Outline (generated)") { hideFlags = HideFlags.HideAndDontSave };
        child.transform.SetParent(transform, false);
        quad = new Mesh { name = "Sprite outline quad", hideFlags = HideFlags.HideAndDontSave };
        quad.vertices = new[] { new Vector3(-.5f, -.5f, 0f), new Vector3(.5f, -.5f, 0f), new Vector3(.5f, .5f, 0f), new Vector3(-.5f, .5f, 0f) };
        quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        quad.RecalculateBounds();
        child.AddComponent<MeshFilter>().sharedMesh = quad;
        outlineRenderer = child.AddComponent<MeshRenderer>();
        outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        outlineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        SyncOutline();
    }

    private void LateUpdate() => SyncOutline();

    private void SyncOutline()
    {
        if (outlineRenderer == null) return;
        outlineRenderer.enabled = source.enabled && !source.forceRenderingOff && source.sprite != null
            && outlineMaterial != null && outlineWidth > 0f && outlineColor.a > 0f && source.color.a > 0f;
        if (!outlineRenderer.enabled) return;

        SpriteData data = GetSpriteData(source.sprite);
        outlineRenderer.sharedMaterial = outlineMaterial;
        outlineRenderer.sortingLayerID = source.sortingLayerID;
        outlineRenderer.sortingOrder = source.sortingOrder;
        outlineRenderer.gameObject.layer = gameObject.layer;
        properties.SetTexture(MainTex, data.texture);
        properties.SetTexture(AlphaTex, data.alphaTexture != null ? data.alphaTexture : Texture2D.whiteTexture);
        properties.SetFloat(ExternalAlpha, data.alphaTexture != null ? 1f : 0f);
        properties.SetVector(Metrics, data.metrics);
        properties.SetVector(UVOrigin, data.uvOrigin);
        properties.SetVector(UVPixelX, data.uvPixelX);
        properties.SetVector(UVPixelY, data.uvPixelY);
        properties.SetFloat(PixelsPerUnit, data.pixelsPerUnit);
        properties.SetVector(Flip, new Vector4(source.flipX ? -1f : 1f, source.flipY ? -1f : 1f, 0f, 0f));
        properties.SetFloat(Opacity, source.color.a);
        properties.SetColor(OutlineColor, outlineColor);
        properties.SetFloat(OutlineWidth, outlineWidth);
        outlineRenderer.SetPropertyBlock(properties);

        Vector2 min = (-new Vector2(data.metrics.z, data.metrics.w) - Vector2.one * outlineWidth) / data.pixelsPerUnit;
        Vector2 max = (new Vector2(data.metrics.x - data.metrics.z, data.metrics.y - data.metrics.w) + Vector2.one * outlineWidth) / data.pixelsPerUnit;
        if (source.flipX) { float oldMin = min.x; min.x = -max.x; max.x = -oldMin; }
        if (source.flipY) { float oldMin = min.y; min.y = -max.y; max.y = -oldMin; }
        Vector2 center = source.spriteSortPoint == SpriteSortPoint.Pivot ? Vector2.zero : (min + max) * .5f;
        Vector2 extents = new Vector2(Mathf.Max(Mathf.Abs(min.x - center.x), Mathf.Abs(max.x - center.x)),
            Mathf.Max(Mathf.Abs(min.y - center.y), Mathf.Abs(max.y - center.y)));
        // MeshRenderer sorts by bounds center; put it at the same pivot as the source sprite.
        outlineRenderer.localBounds = new Bounds(center, new Vector3(extents.x * 2f, extents.y * 2f, .01f));
    }

    private static SpriteData GetSpriteData(Sprite sprite)
    {
        if (SpriteCache.TryGetValue(sprite, out SpriteData data)) return data;
        Vector2[] vertices = sprite.vertices;
        Vector2[] uv = sprite.uv;
        ushort[] triangles = sprite.triangles;
        int a = 0, b = 0, c = 0;
        float largestArea = 0f;
        // Imported tight meshes can begin with degenerate triangles; use a stable UV basis.
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector2 x = vertices[triangles[i + 1]] - vertices[triangles[i]];
            Vector2 y = vertices[triangles[i + 2]] - vertices[triangles[i]];
            float area = Mathf.Abs(x.x * y.y - x.y * y.x);
            if (area <= largestArea) continue;
            largestArea = area;
            a = triangles[i]; b = triangles[i + 1]; c = triangles[i + 2];
        }
        Vector2 origin, pixelX, pixelY;
        if (largestArea > Mathf.Epsilon)
        {
            Vector2 ab = vertices[b] - vertices[a], ac = vertices[c] - vertices[a];
            Vector2 uvAB = uv[b] - uv[a], uvAC = uv[c] - uv[a];
            float determinant = ab.x * ac.y - ab.y * ac.x;
            Vector2 uvX = (uvAB * ac.y - uvAC * ab.y) / determinant;
            Vector2 uvY = (uvAC * ab.x - uvAB * ac.x) / determinant;
            Vector2 offset = -sprite.pivot / sprite.pixelsPerUnit - vertices[a];
            origin = uv[a] + uvX * offset.x + uvY * offset.y;
            pixelX = uvX / sprite.pixelsPerUnit; pixelY = uvY / sprite.pixelsPerUnit;
        }
        else
        {
            Vector4 rect = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            origin = new Vector2(rect.x, rect.y);
            pixelX = new Vector2((rect.z - rect.x) / sprite.rect.width, 0f);
            pixelY = new Vector2(0f, (rect.w - rect.y) / sprite.rect.height);
        }
        data = new SpriteData {
            metrics = new Vector4(sprite.rect.width, sprite.rect.height, sprite.pivot.x, sprite.pivot.y),
            uvOrigin = origin, uvPixelX = pixelX, uvPixelY = pixelY,
            texture = sprite.texture, alphaTexture = sprite.associatedAlphaSplitTexture, pixelsPerUnit = sprite.pixelsPerUnit
        };
        SpriteCache.Add(sprite, data);
        return data;
    }

    private void OnDisable()
    {
        if (outlineRenderer != null) Release(outlineRenderer.gameObject);
        if (quad != null) Release(quad);
        outlineRenderer = null;
        quad = null;
    }

    private static void Release(Object value)
    {
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
