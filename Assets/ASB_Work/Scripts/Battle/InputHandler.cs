using System;
using UnityEngine;
using UnityEngine.EventSystems;
using GridCellRef = ASB.Work.BattleGrid.GridCell;
using GridManagerRef = ASB.Work.BattleGrid.GridManager;

/// <summary>
/// 적 타겟 선택(클릭), 타겟 아웃라인, 숫자키로 BattleManager 실행 요청. 데미지 계산은 하지 않습니다.
/// </summary>
[DisallowMultipleComponent]
public class InputHandler : MonoBehaviour
{
    public static event Action<BattleCharactor, BattleCharactor> PlayerSkillActionResolved;

    [Header("Raycast")]
    [SerializeField] private Camera raycastCamera;
    [SerializeField] private float maxRayDistance = 200f;

    [Header("Target selection")]
    [Tooltip("PlayerGrid, EnemyGrid, Player, Enemy 레이어를 포함한 마스크.")]
    [SerializeField] private LayerMask selectionRaycastMask = ~0;

    [SerializeField] private BattleFlowManager battleFlowManager;
    [SerializeField] private BattleManager battleManager;

    private BattleCharactor selectedTarget;
    private Outline selectedTargetOutline;

    private void Awake()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        PruneDeadSelection();

        if (Input.GetMouseButtonDown(0))
        {
            TrySelectEnemyByClick();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            TryExecuteBasicAttack();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            TryExecuteSkill();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            TryExecuteWeaponSkill();
        }
    }

    /// <summary>BattleFlowManager가 턴 경계에서 호출해 선택 상태를 비웁니다.</summary>
    public void ClearSelectionState()
    {
        SetTargetOutline(null);
    }

    private void PruneDeadSelection()
    {
        if (selectedTarget == null)
        {
            return;
        }

        if (selectedTarget.IsDead || selectedTarget.CurrentHp <= 0f)
        {
            ClearSelectionState();
        }
    }

    private void TrySelectEnemyByClick()
    {
        if (raycastCamera == null)
        {
            Debug.LogWarning("[InputHandler] Raycast용 Camera가 없습니다.");
            return;
        }

        BattleCharactor actor = battleFlowManager != null ? battleFlowManager.CurrentUnit : null;
        if (actor == null || !actor.IsPlayer || actor.IsDead)
        {
            return;
        }

        var ray = raycastCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, selectionRaycastMask);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            BattleCharactor hitUnit = hits[i].collider != null
                ? hits[i].collider.GetComponentInParent<BattleCharactor>()
                : null;

            if (hitUnit == null)
            {
                continue;
            }

            if (hitUnit.IsPlayer)
            {
                continue;
            }

            if (hitUnit.IsDead || hitUnit.CurrentHp <= 0f)
            {
                continue;
            }

            SetTargetOutline(hitUnit);
            return;
        }
    }

    private void TryExecuteBasicAttack()
    {
        BattleCharactor actor = battleFlowManager != null ? battleFlowManager.CurrentUnit : null;
        if (actor == null || !actor.IsPlayer || actor.IsDead)
        {
            return;
        }

        if (selectedTarget == null || battleManager == null)
        {
            return;
        }

        if (selectedTarget.IsDead || selectedTarget.CurrentHp <= 0f)
        {
            ClearSelectionState();
            return;
        }

        if (!TryResolveCell(actor, out _) || !TryResolveCell(selectedTarget, out _))
        {
            Debug.LogWarning("[InputHandler] actor 또는 target의 GridCell을 찾지 못했습니다.");
            return;
        }

        //Vector2Int relative = targetCell.Coords - actorCell.Coords;
        //if (!IsAdjacent4(relative))
        //{
        //    return;
        //}

        if (battleManager.ExecuteBasicAttack(actor, selectedTarget))
        {
            PlayerSkillActionResolved?.Invoke(actor, selectedTarget);
            ClearSelectionState();
        }
    }

    private void TryExecuteSkill()
    {
        BattleCharactor actor = battleFlowManager != null ? battleFlowManager.CurrentUnit : null;
        if (actor == null || !actor.IsPlayer || actor.IsDead)
        {
            return;
        }

        if (!TryGetSelectedSkill(actor, out SkillData skillData))
        {
            Debug.LogWarning($"[InputHandler] 선택된 CSV 스킬이 없습니다: actor={actor.UnitName}");
            return;
        }

        if (selectedTarget == null || battleManager == null)
        {
            return;
        }

        if (selectedTarget.IsDead || selectedTarget.CurrentHp <= 0f)
        {
            ClearSelectionState();
            return;
        }

        if (!TryResolveCell(actor, out GridCellRef actorCell) || !TryResolveCell(selectedTarget, out GridCellRef targetCell))
        {
            Debug.LogWarning("[InputHandler] actor 또는 target의 GridCell을 찾지 못했습니다.");
            return;
        }

        // 이번 단계에서는 단일 타겟 + 데미지형 스킬만 연결하고 boundary 판정은 제외합니다.

        if (battleManager.ExecuteGridSkill(actor, selectedTarget, skillData))
        {
            PlayerSkillActionResolved?.Invoke(actor, selectedTarget);
            ClearSelectionState();
        }
    }

    private void TryExecuteWeaponSkill()
    {
        BattleCharactor actor = battleFlowManager != null ? battleFlowManager.CurrentUnit : null;
        if (actor == null || !actor.IsPlayer || actor.IsDead)
        {
            return;
        }

        if (selectedTarget == null || battleManager == null)
        {
            return;
        }

        if (selectedTarget.IsDead || selectedTarget.CurrentHp <= 0f)
        {
            ClearSelectionState();
            return;
        }

        WeaponData weapon = actor.EquippedWeaponData;
        if (weapon == null)
        {
            Debug.LogWarning($"[InputHandler] 장착 무기가 없어 무기 스킬을 사용할 수 없습니다: actor={actor.UnitName}");
            return;
        }

        SkillData convertedSkill = weapon.ToSkillData();
        if (convertedSkill == null)
        {
            Debug.LogWarning($"[InputHandler] 무기 스킬 변환 실패: weapon={weapon.WeaponName}");
            return;
        }

        if (!TryResolveCell(actor, out GridCellRef actorCell) || !TryResolveCell(selectedTarget, out GridCellRef targetCell))
        {
            Debug.LogWarning("[InputHandler] actor 또는 target의 GridCell을 찾지 못했습니다.");
            return;
        }

        Vector2Int relative = targetCell.Coords - actorCell.Coords;
        if (convertedSkill.boundary != null && convertedSkill.boundary.Count > 0 && !convertedSkill.boundary.Contains(relative))
        {
            return;
        }

        if (battleManager.ExecuteGridSkill(actor, selectedTarget, convertedSkill))
        {
            PlayerSkillActionResolved?.Invoke(actor, selectedTarget);
            ClearSelectionState();
        }
    }

    private static bool IsAdjacent4(Vector2Int relative)
    {
        return Mathf.Abs(relative.x) + Mathf.Abs(relative.y) == 1;
    }

    private static bool TryResolveCell(BattleCharactor unit, out GridCellRef cell)
    {
        cell = null;
        if (unit == null)
        {
            return false;
        }

        cell = unit.OccupiedCell;
        if (cell == null && GridManagerRef.Instance != null)
        {
            cell = GridManagerRef.Instance.FindCellByUnit(unit);
        }

        return cell != null;
    }

    private void SetTargetOutline(BattleCharactor newTarget)
    {
        if (selectedTargetOutline != null)
        {
            selectedTargetOutline.OutlineMode = Outline.Mode.OutlineHidden;
            selectedTargetOutline = null;
        }

        selectedTarget = newTarget;
        if (selectedTarget == null)
        {
            return;
        }

        selectedTargetOutline = selectedTarget.GetComponentInChildren<Outline>(true);
        if (selectedTargetOutline != null)
        {
            selectedTargetOutline.OutlineMode = Outline.Mode.OutlineVisible;
        }
    }

    // 인스펙터에서 선택된 CSV 스킬 조회.
    private bool TryGetSelectedSkill(BattleCharactor actor, out SkillData skillData)
    {
        skillData = null;
        if (actor == null)
        {
            return false;
        }

        actor.ResolveSelectedSkill();
        skillData = actor.SelectedSkillData;
        return skillData != null;
    }

    // TODO: 적 스킬 인스펙터 선택 미구현
    // 적 스킬 저장 방식이 플레이어와 달라 추후 별도 추가 예정
    // TODO: 적 AI 스킬 연결 미구현
    // SelectedSkillData 또는 스킬 슬롯 조회 후
    // 범위 내 타겟 탐색 -> ExecuteGridSkill 호출 경로 추가 필요
}
