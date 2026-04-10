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
    [SerializeField] private Material claimedMaterial;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        ApplyStateMaterial();
    }

    public void MineClaim()
    {
        if(mineState == MineState.Unclaimed)
        {
            mineState = MineState.Claimed;
            ApplyStateMaterial();
            MineClaimed?.Invoke(this);
        }
    }

    public void ProduceForTurn(ResourceManager resourceManager)
    {
        if (mineState != MineState.Claimed)
            return;

        if (resourceManager == null || resourcePerTurn <= 0)
            return;

        resourceManager.AddResource(resourceType, resourcePerTurn);
    }

    private void ApplyStateMaterial()
    {
        if (targetRenderer == null)
            return;

        Material nextMaterial = mineState == MineState.Claimed ? claimedMaterial : unclaimedMaterial;
        if (nextMaterial == null)
            return;

        targetRenderer.material = nextMaterial;
    }
}
