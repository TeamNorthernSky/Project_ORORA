using System;
using System.Collections.Generic;
using UnityEngine;

public class Mine : MonoBehaviour
{
    public static event Action<Mine> MineClaimed;

    [Header("Data")]
    [SerializeField] private MineType mineType = MineType.Bank;
    public int resourcePerTurn;
    public MineState mineState = MineState.Unclaimed;

    [Header("Visual")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material unclaimedMaterial;
    [SerializeField] private Material enemyClaimedMaterial;
    [SerializeField] private Material claimedMaterial;
    private MultiGridOccupant multiGridOccupant;
    private MineRegistry mineRegistry;

    public bool IsClaimableByPlayer => mineState == MineState.Unclaimed || mineState == MineState.EnemyClaimed;
    public bool IsPlayerClaimed => mineState == MineState.Claimed;
    public bool IsEnemyClaimed => mineState == MineState.EnemyClaimed;
    public MineType MineType => mineType;

    private void Awake()
    {
        multiGridOccupant = GetComponent<MultiGridOccupant>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        ApplyStateMaterial();
    }

    private void OnEnable()
    {
        ResolveRegistry();
        mineRegistry?.Register(this);
    }

    private void OnDisable()
    {
        mineRegistry?.Unregister(this);
    }

    public void MineClaim()
    {
        if (IsClaimableByPlayer)
        {
            mineState = MineState.Claimed;
            ApplyStateMaterial();
            MineClaimed?.Invoke(this);
        }
    }

    public void EnemyClaim()
    {
        if (mineState == MineState.EnemyClaimed)
            return;

        mineState = MineState.EnemyClaimed;
        ApplyStateMaterial();
    }

    public void ProduceForTurn(ResourceManager resourceManager)
    {
        if (!IsPlayerClaimed)
            return;

        if (resourceManager == null || resourcePerTurn <= 0)
            return;

        switch (mineType)
        {
            case MineType.Bank:
                resourceManager.AddResource(ResourceType.Money, resourcePerTurn);
                break;
            case MineType.Composite:
                resourceManager.AddResource(ResourceType.Chip, resourcePerTurn);
                resourceManager.AddResource(ResourceType.Crystal, resourcePerTurn);
                resourceManager.AddResource(ResourceType.Supply, resourcePerTurn);
                break;
        }
    }

    public void ApplyInitialData(int nextResourcePerTurn, MineState nextState)
    {
        resourcePerTurn = nextResourcePerTurn;
        mineState = nextState;
        ApplyStateMaterial();
    }

    private void ApplyStateMaterial()
    {
        if (targetRenderer == null)
            return;

        Material nextMaterial = mineState switch
        {
            MineState.Claimed => claimedMaterial,
            MineState.EnemyClaimed => enemyClaimedMaterial,
            _ => unclaimedMaterial
        };
        if (nextMaterial == null)
            return;

        targetRenderer.material = nextMaterial;
    }

    private void ResolveRegistry()
    {
        if (mineRegistry == null)
            mineRegistry = FindFirstObjectByType<MineRegistry>();
    }

    public Vector2Int GetAnchorGrid(GridManager gridManager)
    {
        if (multiGridOccupant != null)
            return multiGridOccupant.AnchorGrid;

        return gridManager != null
            ? gridManager.WorldToGrid(transform.position)
            : Vector2Int.zero;
    }

    public bool OccupiesGrid(Vector2Int grid, GridManager gridManager)
    {
        if (multiGridOccupant != null)
            return multiGridOccupant.OccupiesCell(grid);

        return GetAnchorGrid(gridManager) == grid;
    }

    public IReadOnlyList<Vector2Int> GetAdjacentInteractionCells(GridManager gridManager)
    {
        if (multiGridOccupant != null)
            return multiGridOccupant.GetAdjacentOuterCells();

        Vector2Int anchorGrid = GetAnchorGrid(gridManager);
        List<Vector2Int> adjacentCells = new List<Vector2Int>(GridManager.Directions8.Length);
        for (int i = 0; i < GridManager.Directions8.Length; i++)
            adjacentCells.Add(anchorGrid + GridManager.Directions8[i]);

        return adjacentCells;
    }

    public string GetMineTypeDisplayName()
    {
        return mineType switch
        {
            MineType.Bank => "Bank",
            MineType.Composite => "Composite",
            _ => mineType.ToString()
        };
    }

    public string GetProductionDisplayText()
    {
        return mineType switch
        {
            MineType.Bank => $"Money +{resourcePerTurn} / turn",
            MineType.Composite => $"Chip +{resourcePerTurn}, Crystal +{resourcePerTurn}, Supply +{resourcePerTurn} / turn",
            _ => string.Empty
        };
    }
}
