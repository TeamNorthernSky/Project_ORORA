using System;

[Serializable]
public class UnitPersistentData
{
    public int UnitIndex => unitIndex;

    [UnityEngine.SerializeField] private int unitIndex;

    public UnitPersistentData(int unitIndex)
    {
        this.unitIndex = unitIndex;
    }
}
