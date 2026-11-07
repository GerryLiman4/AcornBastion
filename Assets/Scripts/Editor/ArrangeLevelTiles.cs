using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Lays out a level's tiles to match its art: EnemyPathTile along the brown path sprite and
/// BuildingTile over the remaining grass. Edits the open scene with Undo support and leaves
/// saving to the user.
/// </summary>
public static class ArrangeLevelTiles
{
    private const string TilePrefabPath = "Assets/Game/Prefab/Tile/Tile.prefab";

    private sealed class Layout
    {
        public string SceneName;
        public float FirstColumnX;
        public int ColumnCount;
        public float FirstRowY;
        public int RowCount;
        public Vector2[] PathCorners;
        public Vector2 Spawn;

        // Grass cells that are mostly brown, where a tower would appear to stand on the path.
        public Vector2[] SkippedCells = new Vector2[0];
    }

    // The bgOverlay border leaves a grass opening of x -9.09..9.27, y -5.10..5.08. Each grid is
    // aligned to its own path, then limited to the rows and columns that stay on the grass.

    // The path is a straight band centred on y = 0, ending at x = 6.12.
    private static readonly Layout Level1 = new Layout
    {
        SceneName = "Level 1",
        FirstColumnX = -8.5f,
        ColumnCount = 18,
        FirstRowY = -4f,
        RowCount = 9,
        PathCorners = new[] { new Vector2(-8.5f, 0f), new Vector2(5.5f, 0f) },
        Spawn = new Vector2(-10.5f, 0f),
    };

    // The path enters at y = 2.5, runs down x = 6, then returns along y = -2.5 to the base.
    // Its first tile sits partly on the border so the path reaches the edge of the grass.
    private static readonly Layout Level2 = new Layout
    {
        SceneName = "Level 2",
        FirstColumnX = -8f,
        ColumnCount = 17,
        FirstRowY = -4.5f,
        RowCount = 10,
        PathCorners = new[] { new Vector2(-9f, 2.5f), new Vector2(6f, 2.5f), new Vector2(6f, -2.5f), new Vector2(-3f, -2.5f) },
        Spawn = new Vector2(-10.5f, 2.5f),
    };

    // The path enters on the left, drops down the left side and along the bottom, climbs and
    // crosses the middle, loops over the top, then falls to the base at the lower right.
    private static readonly Layout Level3 = new Layout
    {
        SceneName = "Level 3",
        FirstColumnX = -8.5f,
        ColumnCount = 18,
        FirstRowY = -4.5f,
        RowCount = 10,
        PathCorners = new[]
        {
            new Vector2(-8.5f, 3.5f), new Vector2(-6.5f, 3.5f), new Vector2(-6.5f, -3.5f), new Vector2(1.5f, -3.5f),
            new Vector2(1.5f, -0.5f), new Vector2(-3.5f, -0.5f), new Vector2(-3.5f, 2.5f), new Vector2(3.5f, 2.5f),
            new Vector2(3.5f, -2.5f), new Vector2(6.5f, -2.5f),
        },
        Spawn = new Vector2(-10.5f, 3.5f),
        SkippedCells = new[] { new Vector2(-5.5f, 3.5f), new Vector2(-5.5f, 2.5f), new Vector2(-5.5f, 1.5f), new Vector2(0.5f, -1.5f) },
    };

    // The path enters from the top edge, runs down and around the outside, turns back along the
    // middle, circles the inner loop and crosses itself at (2.5, 0.5) on its way up to the base.
    private static readonly Layout Level4 = new Layout
    {
        SceneName = "Level 4",
        FirstColumnX = -8.5f,
        ColumnCount = 18,
        FirstRowY = -4.5f,
        RowCount = 10,
        PathCorners = new[]
        {
            new Vector2(-6.5f, 4.5f), new Vector2(-6.5f, -3.5f), new Vector2(5.5f, -3.5f), new Vector2(5.5f, 0.5f),
            new Vector2(-2.5f, 0.5f), new Vector2(-2.5f, -1.5f), new Vector2(2.5f, -1.5f), new Vector2(2.5f, 3.5f),
            new Vector2(6.5f, 3.5f),
        },
        Spawn = new Vector2(-6.5f, 6.5f),
        SkippedCells = new[]
        {
            new Vector2(3.5f, 2.5f), new Vector2(-2.5f, 1.5f), new Vector2(-1.5f, 1.5f), new Vector2(-0.5f, 1.5f),
            new Vector2(0.5f, 1.5f), new Vector2(1.5f, 1.5f), new Vector2(3.5f, 1.5f), new Vector2(4.5f, 1.5f),
        },
    };

    [MenuItem("FUR/Arrange Level 1 Tiles", true)]
    [MenuItem("FUR/Arrange Level 2 Tiles", true)]
    [MenuItem("FUR/Arrange Level 3 Tiles", true)]
    [MenuItem("FUR/Arrange Level 4 Tiles", true)]
    private static bool CanArrange() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem("FUR/Arrange Level 1 Tiles")]
    private static void ArrangeLevel1() => Arrange(Level1);

    [MenuItem("FUR/Arrange Level 2 Tiles")]
    private static void ArrangeLevel2() => Arrange(Level2);

    [MenuItem("FUR/Arrange Level 3 Tiles")]
    private static void ArrangeLevel3() => Arrange(Level3);

    [MenuItem("FUR/Arrange Level 4 Tiles")]
    private static void ArrangeLevel4() => Arrange(Level4);

    private static void Arrange(Layout layout)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != layout.SceneName)
        {
            // Unity asks before discarding unsaved changes in the current scene.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene($"Assets/Scenes/{layout.SceneName}.unity");
        }

        GameObject buildingRoot = GameObject.Find("Grid/BuildingTile");
        GameObject pathRoot = GameObject.Find("Grid/EnemyPathTile");
        GameObject spawn = GameObject.Find("Environment/SpawnLocation");
        LevelManager level = Object.FindFirstObjectByType<LevelManager>();
        BaseController playerBase = Object.FindFirstObjectByType<BaseController>();
        GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TilePrefabPath);
        if (buildingRoot == null || pathRoot == null || spawn == null || level == null || playerBase == null || tilePrefab == null)
        {
            Debug.LogError("[ArrangeLevelTiles] Missing Grid/BuildingTile, Grid/EnemyPathTile, Environment/SpawnLocation, LevelManager, BaseController or the Tile prefab. Nothing was changed.");
            return;
        }

        Undo.SetCurrentGroupName($"Arrange {layout.SceneName} Tiles");
        int undoGroup = Undo.GetCurrentGroup();

        List<Vector2> route = BuildRoute(layout.PathCorners);
        List<GameObject> pathTiles = ArrangePath(pathRoot.transform, tilePrefab, route);

        var occupiedCells = new HashSet<Vector2>(route);
        occupiedCells.UnionWith(layout.SkippedCells);
        Rect baseArea = GetBaseArea(playerBase);
        List<GameObject> buildingTiles = ArrangeBuildings(buildingRoot.transform, tilePrefab, layout, occupiedCells, baseArea, out int created);

        Undo.RecordObject(spawn.transform, "Move spawn");
        spawn.transform.position = layout.Spawn;

        var serializedLevel = new SerializedObject(level);
        WriteArray(serializedLevel.FindProperty("enemyPath"), pathTiles);
        WriteArray(serializedLevel.FindProperty("buildingPathList"), buildingTiles);
        serializedLevel.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[ArrangeLevelTiles] {layout.SceneName}: {pathRoot.transform.childCount} path tiles ({pathTiles.Count} route steps), {buildingTiles.Count} building tiles ({created} new). Press Ctrl+S to keep this, Ctrl+Z to undo.");
    }

    // Walks the corners one cell at a time. Enemies follow this order, so it must run from the
    // spawn side toward the base.
    private static List<Vector2> BuildRoute(Vector2[] corners)
    {
        var route = new List<Vector2> { corners[0] };
        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 cell = corners[i - 1];
            while (cell != corners[i])
            {
                cell = Vector2.MoveTowards(cell, corners[i], 1f);
                route.Add(cell);
            }
        }

        return route;
    }

    // A route that crosses itself lists the shared tile twice, so enemies pass through it on
    // both visits without two tiles being stacked in that cell.
    private static List<GameObject> ArrangePath(Transform root, GameObject prefab, List<Vector2> route)
    {
        List<Transform> existing = Children(root);
        var tileByCell = new Dictionary<Vector2, GameObject>();
        var steps = new List<GameObject>();
        int index = 0;
        foreach (Vector2 cell in route)
        {
            if (!tileByCell.TryGetValue(cell, out GameObject tile))
            {
                Transform transform = index < existing.Count ? existing[index++] : CreatePathTile(root, prefab);
                Place(transform, cell);
                tile = transform.gameObject;
                tileByCell.Add(cell, tile);
            }

            steps.Add(tile);
        }

        for (int i = existing.Count - 1; i >= index; i--) Undo.DestroyObjectImmediate(existing[i].gameObject);
        return steps;
    }

    private static Transform CreatePathTile(Transform root, GameObject prefab)
    {
        Transform tile = CreateTile(root, prefab);
        TilePiece piece = tile.GetComponent<TilePiece>();
        Undo.RecordObject(piece, "Block building on path tile");
        piece.canBeOccupied = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(piece);
        return tile;
    }

    private static Transform CreateTile(Transform root, GameObject prefab)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        Undo.RegisterCreatedObjectUndo(instance, "Create tile");
        instance.name = GameObjectUtility.GetUniqueNameForSibling(root, "Tile");
        return instance.transform;
    }

    private static List<GameObject> ArrangeBuildings(Transform root, GameObject prefab, Layout layout, HashSet<Vector2> occupiedCells, Rect baseArea, out int created)
    {
        List<Transform> existing = Children(root);
        var tiles = new List<GameObject>();
        int index = 0;
        created = 0;

        for (int row = layout.RowCount - 1; row >= 0; row--)
        {
            for (int column = 0; column < layout.ColumnCount; column++)
            {
                var cell = new Vector2(layout.FirstColumnX + column, layout.FirstRowY + row);
                if (occupiedCells.Contains(cell) || baseArea.Contains(cell)) continue;

                Transform tile;
                if (index < existing.Count)
                {
                    tile = existing[index++];
                }
                else
                {
                    tile = CreateTile(root, prefab);
                    created++;
                }

                Place(tile, cell);
                tiles.Add(tile.gameObject);
            }
        }

        for (int i = existing.Count - 1; i >= index; i--) Undo.DestroyObjectImmediate(existing[i].gameObject);
        return tiles;
    }

    private static Rect GetBaseArea(BaseController playerBase)
    {
        // Reads the collider's own values because Collider2D.bounds can be stale outside Play mode.
        BoxCollider2D box = playerBase.GetComponentInChildren<BoxCollider2D>(true);
        if (box == null) return Rect.zero;

        Vector2 centre = box.transform.TransformPoint(box.offset);
        Vector2 size = Vector2.Scale(box.size, box.transform.lossyScale);
        return new Rect(centre - size * 0.5f, size);
    }

    private static void Place(Transform tile, Vector2 cell)
    {
        Undo.RecordObject(tile, "Move tile");
        tile.SetPositionAndRotation(new Vector3(cell.x, cell.y, 0f), Quaternion.identity);
        tile.localScale = Vector3.one;
        PrefabUtility.RecordPrefabInstancePropertyModifications(tile);
    }

    private static List<Transform> Children(Transform root)
    {
        var children = new List<Transform>(root.childCount);
        foreach (Transform child in root) children.Add(child);
        return children;
    }

    private static void WriteArray(SerializedProperty property, List<GameObject> items)
    {
        property.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }
}
