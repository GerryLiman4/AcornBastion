using System.Collections.Generic;
using UnityEngine;

public enum WaveSpell { None, Trap, Burst }

public sealed class WaveSpellController : MonoBehaviour
{
    public WaveSpell Selected { get; private set; }
    public float TrapCooldown => Mathf.Max(0, trapReadyAt - Time.time);
    public float BurstCooldown => Mathf.Max(0, burstReadyAt - Time.time);
    public int ActiveTrapCount { get { traps.RemoveAll(item => item == null || !item.isActiveAndEnabled); return traps.Count; } }
    private LevelManager owner;
    private float trapReadyAt;
    private float burstReadyAt;
    private SpriteRenderer preview;
    private readonly HashSet<TilePiece> pathTiles = new HashSet<TilePiece>();
    private readonly List<DamageTrap> traps = new List<DamageTrap>();
    private Bounds mapBounds;

    public void Initialize(LevelManager level)
    {
        owner = level;
        bool first = true;
        foreach (GameObject tileObject in level.enemyPath)
        {
            TilePiece tile = tileObject.GetComponent<TilePiece>();
            if (tile != null) pathTiles.Add(tile);
        }
        foreach (GameObject tile in level.buildingPathList)
        {
            Bounds bounds = TileBounds(tile.GetComponent<TilePiece>());
            if (first) { mapBounds = bounds; first = false; }
            else mapBounds.Encapsulate(bounds);
        }
        foreach (TilePiece tile in pathTiles) mapBounds.Encapsulate(TileBounds(tile));
        preview = CampaignEffects.Sprite("SpellPreview", transform, Vector3.zero, Vector2.one, Color.cyan);
        preview.gameObject.SetActive(false);
    }

    public void Select(WaveSpell spell)
    {
        if (owner.currentState != GameStateId.Wave || owner.isGameOver) return;
        if (spell == WaveSpell.Burst && owner.LevelNumber < owner.Config.burst.unlockLevel) return;
        Selected = Selected == spell ? WaveSpell.None : spell;
        owner.RefreshHud();
    }

    private void Update()
    {
        if (owner == null) return;
        if (owner.currentState != GameStateId.Wave) { Selected = WaveSpell.None; preview.gameObject.SetActive(false); return; }
        if (Input.GetKeyDown(KeyCode.Alpha1)) Select(WaveSpell.Trap);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Select(WaveSpell.Burst);
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) { Selected = WaveSpell.None; owner.RefreshHud(); }
        bool showing = Selected != WaveSpell.None && !PointerUIBlocker.IsPointerOverUI() && Camera.main != null;
        preview.gameObject.SetActive(showing);
        if (!showing) return;
        Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        position.z = 0;
        TilePiece tile = FindPathTile(position);
        bool valid;
        if (Selected == WaveSpell.Trap)
        {
            Bounds bounds = tile != null ? TileBounds(tile) : new Bounds(position, Vector3.one);
            preview.sprite = CampaignEffects.Square;
            preview.transform.position = bounds.center;
            preview.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1);
            valid = CanCastTrap(tile);
        }
        else
        {
            preview.sprite = CampaignEffects.Ring;
            preview.transform.position = position;
            preview.transform.localScale = Vector3.one * owner.Config.burst.radius * 2;
            valid = CanCastBurst(position);
        }
        preview.color = valid ? new Color(0.2f, 1f, 0.9f, 0.55f) : new Color(1f, 0.2f, 0.2f, 0.45f);
        if (!Input.GetMouseButtonDown(0)) return;
        bool cast = Selected == WaveSpell.Trap ? TryCastTrap(tile) : TryCastBurst(position);
        if (cast) { Selected = WaveSpell.None; preview.gameObject.SetActive(false); owner.RefreshHud(); }
    }

    public static Bounds TileBounds(TilePiece tile)
    {
        Collider2D collider = tile != null ? tile.GetComponent<Collider2D>() : null;
        if (collider != null) return collider.bounds;
        return new Bounds(tile != null ? tile.transform.position : Vector3.zero, Vector3.one);
    }

    private TilePiece FindPathTile(Vector2 position)
    {
        foreach (TilePiece tile in pathTiles)
        {
            Bounds bounds = TileBounds(tile);
            if (position.x >= bounds.min.x && position.x <= bounds.max.x
                && position.y >= bounds.min.y && position.y <= bounds.max.y) return tile;
        }
        return null;
    }

    public bool CanCastTrap(TilePiece tile)
    {
        if (owner.currentState != GameStateId.Wave || owner.isGameOver || tile == null || !pathTiles.Contains(tile)
            || owner.Mana < owner.Config.trap.cost || TrapCooldown > 0 || ActiveTrapCount >= owner.Config.trap.maxActive) return false;
        return !traps.Exists(item => item != null && item.Tile == tile);
    }

    public bool TryCastTrap(TilePiece tile)
    {
        if (!CanCastTrap(tile) || !owner.TrySpendMana(owner.Config.trap.cost)) return false;
        trapReadyAt = Time.time + owner.Config.trap.cooldown;
        Bounds bounds = TileBounds(tile);
        DamageTrap trap = Instantiate(owner.damageTrapPrefab, bounds.center, Quaternion.identity);
        trap.Initialize(owner, tile, bounds);
        traps.Add(trap);
        owner.RefreshHud();
        return true;
    }

    public bool CanCastBurst(Vector2 position)
    {
        return owner.currentState == GameStateId.Wave && !owner.isGameOver
            && owner.LevelNumber >= owner.Config.burst.unlockLevel && owner.Mana >= owner.Config.burst.cost
            && BurstCooldown <= 0 && position.x >= mapBounds.min.x && position.x <= mapBounds.max.x
            && position.y >= mapBounds.min.y && position.y <= mapBounds.max.y;
    }

    public bool TryCastBurst(Vector2 position)
    {
        if (!CanCastBurst(position) || !owner.TrySpendMana(owner.Config.burst.cost)) return false;
        burstReadyAt = Time.time + owner.Config.burst.cooldown;
        float radius = owner.Config.burst.radius;
        SpriteRenderer flash = CampaignEffects.Sprite("AreaBurst", null, position, Vector2.one * radius * 2,
            new Color(1f, 0.75f, 0.2f, 0.95f), true);
        Destroy(flash.gameObject, 0.3f);
        // Kills remove enemies from the owner's live list, so iterate a snapshot.
        foreach (CreepController enemy in owner.creepControllerList.ToArray())
            if (enemy != null && !enemy.IsResolved && enemy.hpController.CanBeTargeted()
                && ((Vector2)enemy.transform.position - position).sqrMagnitude <= radius * radius)
                enemy.hpController.GetDamaged(gameObject, owner.Config.burst.damage);
        owner.RefreshHud();
        return true;
    }

    public void EndWave()
    {
        Selected = WaveSpell.None;
        trapReadyAt = burstReadyAt = 0;
        if (preview != null) preview.gameObject.SetActive(false);
        foreach (DamageTrap trap in traps)
            if (trap != null) { trap.gameObject.SetActive(false); Destroy(trap.gameObject); }
        traps.Clear();
    }

    private void OnDestroy() => EndWave();
}
