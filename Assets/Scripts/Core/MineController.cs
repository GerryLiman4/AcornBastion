using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MineController : MonoBehaviour
{
    [SerializeField] public List<TilePiece> tilePieceList = new List<TilePiece>();
    [SerializeField] public MineralObject mineralPrefab;
    private readonly List<MineralObject> minerals = new List<MineralObject>();
    private LevelManager owner;
    public int RemainingCount
    {
        get
        {
            int count = 0;
            foreach (MineralObject mineral in minerals) if (mineral != null && !mineral.IsCollected) count++;
            return count;
        }
    }

    public void SetOwner(LevelManager level) => owner = level;

    public void Initalize(GameObject[] designatedPieceList)
    {
        tilePieceList.Clear();
        foreach (GameObject piece in designatedPieceList)
        {
            TilePiece tile = piece.GetComponent<TilePiece>();
            tilePieceList.Add(tile);
        }
    }

    public IEnumerator SpawnRandomMinerals(int quantity)
    {
        SpawnMinerals(quantity, 0);
        yield break;
    }

    public void SpawnMinerals(int quantity, int manaCount)
    {
        ClearMinerals();
        List<TilePiece> availableTiles = new List<TilePiece>();
        foreach (TilePiece tile in tilePieceList)
        {
            if (CanSpawnOnTile(tile) && !availableTiles.Contains(tile))
            {
                availableTiles.Add(tile);
            }
        }

        int spawned = 0;
        while (spawned < quantity && availableTiles.Count > 0)
        {
            int randomIndex = Random.Range(0, availableTiles.Count);
            TilePiece tile = availableTiles[randomIndex];
            availableTiles.RemoveAt(randomIndex);

            // Another spawn or tower placement may have claimed this tile since the last frame.
            if (!CanSpawnOnTile(tile)) continue;

            MineralObject obj = Instantiate(mineralPrefab, tile.root.transform);
            tile.OccupyTile(obj.gameObject);
            // OccupyTile snaps to the tile root; restore the prefab offset so minerals keep their depth (Z -0.25).
            obj.transform.localPosition = mineralPrefab.transform.localPosition;
            obj.SetParentTile(tile);
            obj.Initialize(owner, spawned < manaCount);
            minerals.Add(obj);
            spawned++;

        }
        if (spawned != quantity) Debug.LogError($"Only {spawned}/{quantity} mining tiles available.", this);
    }

    public void ClearMinerals()
    {
        foreach (MineralObject mineral in minerals)
            if (mineral != null) mineral.Discard();
        minerals.Clear();
    }

    private static bool CanSpawnOnTile(TilePiece tile)
    {
        return tile != null && tile.canBeOccupied && !tile.isOccupied
            && tile.root != null && tile.root.transform.childCount == 0;
    }
}
