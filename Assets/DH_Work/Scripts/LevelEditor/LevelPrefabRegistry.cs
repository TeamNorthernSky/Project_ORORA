using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelPrefabRegistry : MonoBehaviour
{
    [Header("Default Prefabs")]
    [SerializeField] private GameObject obstaclePrefab;

    [Header("Item Prefabs")]
    [SerializeField] private List<ItemPrefabEntry> itemPrefabs = new List<ItemPrefabEntry>();

    [Header("Mine Prefabs")]
    [SerializeField] private List<MinePrefabEntry> minePrefabs = new List<MinePrefabEntry>();

    public GameObject ObstaclePrefab => obstaclePrefab;

    public bool TryGetItemPrefab(ResourceType resourceType, out ItemObject prefab)
    {
        for (int i = 0; i < itemPrefabs.Count; i++)
        {
            if (itemPrefabs[i].ResourceType != resourceType)
                continue;

            prefab = itemPrefabs[i].Prefab;
            return prefab != null;
        }

        prefab = null;
        return false;
    }

    public bool TryGetMinePrefab(ResourceType resourceType, out Mine prefab)
    {
        for (int i = 0; i < minePrefabs.Count; i++)
        {
            if (minePrefabs[i].ResourceType != resourceType)
                continue;

            prefab = minePrefabs[i].Prefab;
            return prefab != null;
        }

        prefab = null;
        return false;
    }
}

[Serializable]
public struct ItemPrefabEntry
{
    [SerializeField] private ResourceType resourceType;
    [SerializeField] private ItemObject prefab;

    public ResourceType ResourceType => resourceType;
    public ItemObject Prefab => prefab;
}

[Serializable]
public struct MinePrefabEntry
{
    [SerializeField] private ResourceType resourceType;
    [SerializeField] private Mine prefab;

    public ResourceType ResourceType => resourceType;
    public Mine Prefab => prefab;
}
