using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    public event System.Action<EnemyUnit, Vector2Int> GridChanged;
    public event System.Action<EnemyUnit, Vector2Int> MoveStepStarted;

    [Header("Identity")]
    [SerializeField] private string enemyId = "enemy_001";
    [SerializeField] private List<int> combatUnitIndices = new List<int>();

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PersistentEnemyRepository persistentEnemyRepository;

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float arriveThreshold = 0.01f;
    [SerializeField] private int movePointsPerTurn = 5;

    [Header("Targeting")]
    [SerializeField] private int detectionRange = 10;
    [SerializeField] private EnemyTargetingProfile targetingProfile = EnemyTargetingProfile.Balanced;

    private Vector2Int currentGrid;
    private float fixedY;
    private EnemyTargetType currentTargetType;
    private Component currentTarget;
    private EnemyRegistry enemyRegistry;

    public string EnemyId => enemyId;
    public IReadOnlyList<int> CombatUnitIndices => combatUnitIndices;
    public int MovePointsPerTurn => Mathf.Max(0, movePointsPerTurn);
    public int DetectionRange => Mathf.Max(0, detectionRange);
    public EnemyTargetingProfile TargetingProfile => targetingProfile;
    public int ResourceGroupWeight => GetResourceGroupWeight(targetingProfile);
    public int StrategicGroupWeight => GetStrategicGroupWeight(targetingProfile);
    public EnemyTargetType CurrentTargetType => currentTargetType;
    public Component CurrentTarget => currentTarget;

    public bool HasCombatUnitIndices()
    {
        return combatUnitIndices != null && combatUnitIndices.Count > 0;
    }

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        fixedY = transform.position.y;
        currentGrid = gridManager != null ? gridManager.WorldToGrid(transform.position) : Vector2Int.zero;
    }

    private void OnEnable()
    {
        ResolveRegistry();
        enemyRegistry?.Register(this);
        RegisterPersistentData();
    }

    private void OnDisable()
    {
        enemyRegistry?.Unregister(this);
    }

    public Vector2Int GetCurrentGrid()
    {
        return currentGrid;
    }

    public bool HasTarget()
    {
        return currentTarget != null && currentTargetType != EnemyTargetType.None;
    }

    public void SetTarget(EnemyTargetType targetType, Component target)
    {
        currentTargetType = target != null ? targetType : EnemyTargetType.None;
        currentTarget = target;
    }

    public void ClearTarget()
    {
        currentTargetType = EnemyTargetType.None;
        currentTarget = null;
    }

    public void SnapToGridPosition(Vector2Int grid)
    {
        bool changed = currentGrid != grid;
        currentGrid = grid;

        if (gridManager == null)
        {
            if (changed)
                GridChanged?.Invoke(this, currentGrid);
            return;
        }

        Vector3 worldPosition = gridManager.GridToWorldCenter(grid);
        worldPosition.y = fixedY;
        transform.position = worldPosition;

        if (changed)
            GridChanged?.Invoke(this, currentGrid);
    }

    public IEnumerator MoveAlongPath(List<Vector2Int> path)
    {
        if (path == null || path.Count <= 1 || gridManager == null)
            yield break;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int nextGrid = path[i];
            MoveStepStarted?.Invoke(this, nextGrid);

            Vector3 target = gridManager.GridToWorldCenter(nextGrid);
            target.y = fixedY;

            while ((transform.position - target).sqrMagnitude > arriveThreshold * arriveThreshold)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = target;
            currentGrid = nextGrid;
            GridChanged?.Invoke(this, currentGrid);
        }
    }

    private void ResolveRegistry()
    {
        if (enemyRegistry == null)
            enemyRegistry = FindFirstObjectByType<EnemyRegistry>();

        if (persistentEnemyRepository == null)
            persistentEnemyRepository = PersistentEnemyRepository.Instance;
    }

    private void RegisterPersistentData()
    {
        if (string.IsNullOrWhiteSpace(enemyId))
        {
            Debug.LogWarning("EnemyUnit has no enemyId. Persistent enemy data registration was skipped.", this);
            return;
        }

        persistentEnemyRepository ??= PersistentEnemyRepository.Instance;
        persistentEnemyRepository?.RegisterOrUpdateEnemy(enemyId, combatUnitIndices);
    }

    private static int GetResourceGroupWeight(EnemyTargetingProfile profile)
    {
        switch (profile)
        {
            case EnemyTargetingProfile.Aggressive:
                return 20;
            case EnemyTargetingProfile.Stable:
                return 70;
            case EnemyTargetingProfile.Balanced:
            default:
                return 50;
        }
    }

    private static int GetStrategicGroupWeight(EnemyTargetingProfile profile)
    {
        switch (profile)
        {
            case EnemyTargetingProfile.Aggressive:
                return 80;
            case EnemyTargetingProfile.Stable:
                return 30;
            case EnemyTargetingProfile.Balanced:
            default:
                return 50;
        }
    }
}
