using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemObject : MonoBehaviour
{
    public ResourceType resourceType;
    public int amount;

    public void GetItem(ResourceManager resourceManager)
    {
        Debug.Log($"Get {resourceType} : {amount}");

        resourceManager.AddResource(resourceType, amount);

        Destroy(gameObject);
    }
}
