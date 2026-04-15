using UnityEngine;
using GridCellRef = ASB.Work.BattleGrid.GridCell;

[DisallowMultipleComponent]
public class BattleCharactorFactory : MonoBehaviour
{
    [SerializeField] private Transform defaultParent;

    public BattleCharactor CreateOrInitialize(UnitSpawnData spawnData)
    {
        if (spawnData == null || !spawnData.IsValid)
        {
            return null;
        }

        GameObject root = spawnData.ExistingRoot;
        if (root == null)
        {
            root = Instantiate(spawnData.Prefab, defaultParent);
        }

        if (root == null)
        {
            return null;
        }

        var presenter = GetOrAddComponent<UnitPresenter>(root);
        presenter.SetRuntimeData(spawnData.Data, spawnData.Team, spawnData.GridNumber);

        var battle = GetOrAddComponent<BattleCharactor>(root);
        battle.Initialize(spawnData.Data, spawnData.Team);

        BindTeamHandlers(root, spawnData);
        BindGridCell(battle, spawnData.GridNumber);
        return battle;
    }

    private void BindTeamHandlers(GameObject root, UnitSpawnData spawnData)
    {
        var equipHandler = GetOrAddComponent<PlayerEquipmentHandler>(root);
        var aiHandler = GetOrAddComponent<EnemyAIHandler>(root);

        bool isPlayer = spawnData.Team == TeamType.Player;
        equipHandler.enabled = isPlayer;
        aiHandler.enabled = !isPlayer;

        if (isPlayer)
        {
            equipHandler.Configure(spawnData.Data as PlayerDataSO);
        }
        else
        {
            aiHandler.Configure(spawnData.Data as EnemyDataSO);
        }
    }

    private void BindGridCell(BattleCharactor battle, int gridNumber)
    {
        if (battle == null)
        {
            return;
        }

        var gridObject = GameObject.Find($"Grid_{gridNumber}");
        if (gridObject == null)
        {
            return;
        }

        var cell = gridObject.GetComponent<GridCellRef>();
        if (cell == null)
        {
            return;
        }

        battle.AssignToCell(cell);
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        if (target == null)
        {
            return null;
        }

        var comp = target.GetComponent<T>();
        if (comp == null)
        {
            comp = target.AddComponent<T>();
        }

        return comp;
    }
}
