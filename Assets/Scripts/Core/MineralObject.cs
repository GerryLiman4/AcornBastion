using DG.Tweening;
using UnityEngine;

public class MineralObject : MonoBehaviour
{
    [SerializeField] public SpriteRenderer model;
    [SerializeField] private GameObject manaIndicatorPrefab;
    [Tooltip("One is picked at random for each mineral.")]
    [SerializeField] private Sprite[] modelSprites;
    [Tooltip("Longest side of the model in world units, so every sprite fits its tile whatever its art size.")]
    [SerializeField, Min(0.01f)] private float modelMaxSize = 1f;

    [SerializeField] public int minValue = 15;
    [SerializeField] public int maxValue = 45;
    [SerializeField] public int minTap = 1;
    [SerializeField] public int maxTap = 3;

    public int tapCount;
    public int value;
    public bool HasMana { get; private set; }
    public bool IsCollected { get; private set; }
    private LevelManager owner;

    public float hoverScaleMultiplier = 1.2f;
    public Vector3 originalModelScale;

    public TilePiece parentTile;
    private void Awake()
    {
        originalModelScale = model.transform.localScale;
    }

    public void Initialize()
    {
        Initialize(FindFirstObjectByType<LevelManager>(), false);
    }

    public void Initialize(LevelManager level, bool mana)
    {
        owner = level;
        HasMana = mana;
        IsCollected = false;
        tapCount = level != null ? level.Config.oreClicks : 2;
        value = level != null ? level.Config.oreGold : 20;
        ApplyRandomModel();
        if (mana)
        {
            model.color = new Color(0.3f, 0.85f, 1f);
            Instantiate(manaIndicatorPrefab, transform.position + Vector3.up * 0.65f, Quaternion.identity, transform);
        }
    }

    private void ApplyRandomModel()
    {
        if (modelSprites == null || modelSprites.Length == 0) return;
        Sprite sprite = modelSprites[Random.Range(0, modelSprites.Length)];
        model.sprite = sprite;
        float longestSide = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        float scale = modelMaxSize / longestSide;
        model.transform.localScale = new Vector3(scale, scale, 1f);

        // Keep the click area matched to the sprite that was picked.
        if (TryGetComponent(out PolygonCollider2D clickArea))
        {
            Vector2 centre = sprite.bounds.center * scale;
            Vector2 half = sprite.bounds.extents * scale;
            clickArea.pathCount = 1;
            clickArea.SetPath(0, new[]
            {
                centre + new Vector2(-half.x, -half.y), centre + new Vector2(half.x, -half.y),
                centre + new Vector2(half.x, half.y), centre + new Vector2(-half.x, half.y),
            });
        }
    }

    public bool TryMine()
    {
        if (IsCollected || owner == null || owner.currentState != GameStateId.Deploy) return false;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMineralClick();
        tapCount--;
        if (tapCount > 0) return true;
        IsCollected = true;
        SignalManager.MineralObjectDestroyed(this);
        ReleaseTile();
        Destroy(gameObject);
        return true;
    }

    public void Discard()
    {
        IsCollected = true;
        ReleaseTile();
        Destroy(gameObject);
    }

    private void ReleaseTile()
    {
        transform.SetParent(null, true);
        if (parentTile != null && parentTile.GetOccupator() == null) parentTile.OccupyTile(null);
        gameObject.SetActive(false);
    }

    private Tween scaleTween;
    private void OnMouseEnter()
    {
        scaleTween?.Kill();

        scaleTween = transform
            .DOScale(originalModelScale * hoverScaleMultiplier, 0.25f)
            .SetEase(Ease.OutBack);
    }

    private void OnMouseExit()
    {
        scaleTween?.Kill();

        scaleTween = transform
            .DOScale(originalModelScale, 0.25f)
            .SetEase(Ease.OutQuad);
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
        transform.localScale = originalModelScale;
    }

    private void OnMouseDown()
    {
        if (PointerUIBlocker.IsPointerOverUI()) return;

        if (!TryMine() || IsCollected) return;

        scaleTween?.Kill();

        scaleTween = DOTween.Sequence()
        .Append(transform.DOScale(originalModelScale * 0.9f, 0.08f)
            .SetEase(Ease.OutQuad))
        .Append(transform.DOScale(originalModelScale * hoverScaleMultiplier, 0.2f)
            .SetEase(Ease.OutBack));
    }

    public void SetParentTile(TilePiece designatedTile)
    {
        parentTile = designatedTile;
    }
}
