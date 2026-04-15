using System;
using UnityEngine;

public class Mine : MonoBehaviour
{
    public static event Action<Mine> MineClaimed;

    [Header("Data")]
    public ResourceType resourceType;
    public int resourcePerTurn;
    public MineState mineState = MineState.Unclaimed;

    [Header("Visual")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material unclaimedMaterial;
    [SerializeField] private Material enemyClaimedMaterial;
    [SerializeField] private Material claimedMaterial;

    public bool IsClaimableByPlayer => mineState == MineState.Unclaimed || mineState == MineState.EnemyClaimed;
    public bool IsPlayerClaimed => mineState == MineState.Claimed;
    public bool IsEnemyClaimed => mineState == MineState.EnemyClaimed;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        ApplyStateMaterial();
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

        resourceManager.AddResource(resourceType, resourcePerTurn);
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
}
