using System;
using UnityEngine;

[Serializable]
public class UnitSpawnData
{
    public IUnitData Data;
    public TeamType Team;
    public GameObject Prefab;
    public GameObject ExistingRoot;
    public int GridNumber;

    public bool IsValid => Data != null && (ExistingRoot != null || Prefab != null);
}
