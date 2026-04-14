using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemObject : MonoBehaviour
{
    public ResourceType resourceType;
    public int amount;

    public void ApplyInitialAmount(int nextAmount)
    {
        amount = nextAmount;
    }

    public void GetItem(ResourceManager resourceManager)
    {
        resourceManager.AddResource(resourceType, amount);

        Destroy(gameObject);
    }
}
