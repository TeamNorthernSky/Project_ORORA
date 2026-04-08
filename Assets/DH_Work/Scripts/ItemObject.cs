using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemObject : MonoBehaviour
{
    public ResourceType resourceType;
    public int amount;

    public void GetItem()
    {
        Debug.Log($"Get {resourceType} : {amount}");

        // 나중에 여기서 PlayerResourceManager 연결
        // party.Owner.ResourceManager.Add(resourceType, amount);

        Destroy(gameObject);
    }
}
