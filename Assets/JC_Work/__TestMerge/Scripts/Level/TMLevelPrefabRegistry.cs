using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orora.TestMerge
{
    public class TMLevelPrefabRegistry : MonoBehaviour
    {
        [Header("Default Prefabs")]
        [SerializeField] private GameObject obstaclePrefab;

        [Header("Item Prefabs")]
        [SerializeField] private List<TMItemPrefabEntry> itemPrefabs = new List<TMItemPrefabEntry>();

        [Header("Mine Prefabs")]
        [SerializeField] private List<TMMinePrefabEntry> minePrefabs = new List<TMMinePrefabEntry>();

        public GameObject ObstaclePrefab => obstaclePrefab;

        public bool TryGetItemPrefab(TMResourceType resource, out TMItemObject prefab)
        {
            for (int i = 0; i < itemPrefabs.Count; i++)
            {
                if (itemPrefabs[i].ResourceType != resource) continue;
                prefab = itemPrefabs[i].Prefab;
                return prefab != null;
            }
            prefab = null;
            return false;
        }

        public bool TryGetMinePrefab(TMResourceType resource, out TMMine prefab)
        {
            for (int i = 0; i < minePrefabs.Count; i++)
            {
                if (minePrefabs[i].ResourceType != resource) continue;
                prefab = minePrefabs[i].Prefab;
                return prefab != null;
            }
            prefab = null;
            return false;
        }
    }

    [Serializable]
    public struct TMItemPrefabEntry
    {
        [SerializeField] private TMResourceType resourceType;
        [SerializeField] private TMItemObject prefab;
        public TMResourceType ResourceType => resourceType;
        public TMItemObject Prefab => prefab;
    }

    [Serializable]
    public struct TMMinePrefabEntry
    {
        [SerializeField] private TMResourceType resourceType;
        [SerializeField] private TMMine prefab;
        public TMResourceType ResourceType => resourceType;
        public TMMine Prefab => prefab;
    }
}
