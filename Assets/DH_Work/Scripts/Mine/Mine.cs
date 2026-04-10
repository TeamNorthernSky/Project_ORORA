using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mine : MonoBehaviour
{
    public ResourceType resourceType;
    public MineState mineState;

    public void MineClaim()
    {
        if(mineState == MineState.Unclaimed)
            mineState = MineState.Claimed;
    }
}
