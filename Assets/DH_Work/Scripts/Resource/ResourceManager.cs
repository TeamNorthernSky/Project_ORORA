using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [SerializeField] private int gold;
    [SerializeField] private int wood;
    [SerializeField] private int ore;

    public int GetAmount(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Gold: return gold;
            case ResourceType.Wood: return wood;
            case ResourceType.Ore: return ore;
            default: return 0;
        }
    }

    public void AddResource(ResourceType type, int amount)
    {
        if (amount <= 0) return;

        switch (type)
        {
            case ResourceType.Gold:
                gold += amount;
                break;
            case ResourceType.Wood:
                wood += amount;
                break;
            case ResourceType.Ore:
                ore += amount;
                break;
        }
    }

    public bool HasResource(ResourceType type, int amount)
    {
        if (amount <= 0) return true;
        return GetAmount(type) >= amount;
    }

    public bool SpendResource(ResourceType type, int amount)
    {
        if (amount <= 0) return false;
        if (!HasResource(type, amount)) return false;

        switch (type)
        {
            case ResourceType.Gold:
                gold -= amount;
                break;
            case ResourceType.Wood:
                wood -= amount;
                break;
            case ResourceType.Ore:
                ore -= amount;
                break;
        }

        return true;
    }
}
