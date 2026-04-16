using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string enemyId = "enemy_001";

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private EnemyRegistry enemyRegistry;

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

    public string EnemyId => enemyId;
    public int MovePointsPerTurn => Mathf.Max(0, movePointsPerTurn);
    public int DetectionRange => Mathf.Max(0, detectionRange);
    public EnemyTargetingProfile TargetingProfile => targetingProfile;
    public int ResourceGroupWeight => GetResourceGroupWeight(targetingProfile);
    public int StrategicGroupWeight => GetStrategicGroupWeight(targetingProfile);
    public EnemyTargetType CurrentTargetType => currentTargetType;
    public Component CurrentTarget => currentTarget;

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
        currentGrid = grid;

        if (gridManager == null)
            return;

        Vector3 worldPosition = gridManager.GridToWorldCenter(grid);
        worldPosition.y = fixedY;
        transform.position = worldPosition;
    }

    public IEnumerator MoveAlongPath(List<Vector2Int> path)
    {
        if (path == null || path.Count <= 1 || gridManager == null)
            yield break;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int nextGrid = path[i];
            Vector3 target = gridManager.GridToWorldCenter(nextGrid);
            target.y = fixedY;

            while ((transform.position - target).sqrMagnitude > arriveThreshold * arriveThreshold)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = target;
            currentGrid = nextGrid;
        }
    }

    private void ResolveRegistry()
    {
        if (enemyRegistry == null)
            enemyRegistry = FindFirstObjectByType<EnemyRegistry>();
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
