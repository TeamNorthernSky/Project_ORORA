using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class LevelLoader : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LevelData levelData;

    [Header("Runtime References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PartyRegistry partyRegistry;
    [SerializeField] private LevelPrefabRegistry prefabRegistry;

    [Header("Spawn Roots")]
    [SerializeField] private Transform obstacleRoot;
    [SerializeField] private Transform itemRoot;
    [SerializeField] private Transform mineRoot;

    [Header("Load Options")]
    [SerializeField] private bool loadOnStart;
    [SerializeField] private bool clearExistingBeforeLoad = true;
    [SerializeField] private bool applyInEditMode = true;
    [SerializeField] private bool autoReloadOnValidate = true;

#if UNITY_EDITOR
    private bool queuedEditorReload;
#endif

    private void Start()
    {
        if (loadOnStart)
            LoadLevel();
    }

    private void OnValidate()
    {
        if (!autoReloadOnValidate || Application.isPlaying)
            return;

        QueueEditorReload();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
            return;

        QueueEditorReload();
    }

    [ContextMenu("Load Level")]
    public void LoadLevel()
    {
        if (levelData == null || gridManager == null)
            return;

        if (clearExistingBeforeLoad)
            ClearSpawnedObjects();

        SpawnObstacles();
        SpawnItems();
        SpawnMines();
        ApplyPartySpawns();
    }

    private void TryLoadInEditMode()
    {
        if (!applyInEditMode)
            return;

        if (levelData == null || gridManager == null)
            return;

        LoadLevel();
    }

#if UNITY_EDITOR
    private void QueueEditorReload()
    {
        if (queuedEditorReload)
            return;

        queuedEditorReload = true;
        EditorApplication.delayCall += HandleEditorReload;
    }

    private void HandleEditorReload()
    {
        EditorApplication.delayCall -= HandleEditorReload;
        queuedEditorReload = false;

        if (this == null || !isActiveAndEnabled || Application.isPlaying)
            return;

        TryLoadInEditMode();
    }
#endif

    private void SpawnObstacles()
    {
        GameObject obstaclePrefab = prefabRegistry != null ? prefabRegistry.ObstaclePrefab : null;
        if (obstaclePrefab == null)
            return;

        var obstacleCells = levelData.ObstacleCells;
        for (int i = 0; i < obstacleCells.Count; i++)
        {
            SpawnGameObject(obstaclePrefab, obstacleCells[i], obstacleRoot);
        }
    }

    private void SpawnItems()
    {
        if (prefabRegistry == null)
            return;

        var itemPlacements = levelData.ItemPlacements;
        for (int i = 0; i < itemPlacements.Count; i++)
        {
            ItemPlacementData placement = itemPlacements[i];
            if (!prefabRegistry.TryGetItemPrefab(placement.ResourceType, out ItemObject itemPrefab))
            {
                Debug.LogWarning(
                    $"LevelLoader could not find an item prefab for resource type '{placement.ResourceType}'.",
                    this);
                continue;
            }

            ItemObject item = SpawnComponent(itemPrefab, placement.GridPosition, itemRoot);
            if (item == null)
                continue;

            item.ApplyInitialAmount(placement.Amount);
        }
    }

    private void SpawnMines()
    {
        if (prefabRegistry == null)
            return;

        var minePlacements = levelData.MinePlacements;
        for (int i = 0; i < minePlacements.Count; i++)
        {
            MinePlacementData placement = minePlacements[i];
            if (!prefabRegistry.TryGetMinePrefab(placement.ResourceType, out Mine minePrefab))
            {
                Debug.LogWarning(
                    $"LevelLoader could not find a mine prefab for resource type '{placement.ResourceType}'.",
                    this);
                continue;
            }

            Mine mine = SpawnComponent(minePrefab, placement.GridPosition, mineRoot);
            if (mine == null)
                continue;

            mine.ApplyInitialData(
                placement.ResourcePerTurn,
                placement.InitialState);
        }
    }

    private void ApplyPartySpawns()
    {
        if (partyRegistry == null)
            return;

        var partySpawns = levelData.PartySpawns;
        for (int i = 0; i < partySpawns.Count; i++)
        {
            PartySpawnData spawnData = partySpawns[i];
            if (string.IsNullOrWhiteSpace(spawnData.PartyId))
            {
                Debug.LogWarning("LevelLoader found a party spawn with an empty partyId.", this);
                continue;
            }

            if (!partyRegistry.TryGetPartyById(spawnData.PartyId, out PartyGridMover partyMover))
            {
                Debug.LogWarning(
                    $"LevelLoader could not find a registered party for partyId '{spawnData.PartyId}'.",
                    this);
                continue;
            }

            partyMover.SnapToGridPosition(spawnData.GridPosition);
        }
    }

    private void ClearSpawnedObjects()
    {
        ClearChildren(obstacleRoot);
        ClearChildren(itemRoot);
        ClearChildren(mineRoot);
    }

    private void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject child = root.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private GameObject SpawnGameObject(GameObject prefab, Vector2Int grid, Transform parent)
    {
        if (prefab == null || !levelData.IsInsideGrid(grid))
            return null;

        Vector3 worldPosition = GetWorldPosition(grid);
        return Instantiate(prefab, worldPosition, Quaternion.identity, parent);
    }

    private T SpawnComponent<T>(T prefab, Vector2Int grid, Transform parent) where T : Component
    {
        if (prefab == null || !levelData.IsInsideGrid(grid))
            return null;

        Vector3 worldPosition = GetWorldPosition(grid);
        return Instantiate(prefab, worldPosition, Quaternion.identity, parent);
    }

    private Vector3 GetWorldPosition(Vector2Int grid)
    {
        Vector3 worldPosition = gridManager.GridToWorldCenter(grid);
        worldPosition.y = gridManager.GetLandSurfaceY();
        return worldPosition;
    }
}
