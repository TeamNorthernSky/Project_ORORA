using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 플레이어/적 유닛 클릭 선택. Quick Outline은 OutlineMode(Visible/Hidden)로 전환해 셰이더 유지.
/// </summary>
[DisallowMultipleComponent]
public class InputHandler : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera raycastCamera;
    [Tooltip("레이캐스트에 포함할 모든 레이어(바닥 등). 비어 있으면 모든 레이어")]
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private float maxRayDistance = 200f;

    [Header("Unit Masks")]
    [Tooltip("플레이어 유닛 레이어(단일 레이어만 체크하려면 해당 레이어만 지정)")]
    [SerializeField] private LayerMask playerMask;
    [Tooltip("적 유닛 레이어")]
    [SerializeField] private LayerMask enemyMask;

    private Outline selectedPlayerOutline;
    private Outline selectedEnemyOutline;

    private void Awake()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        if (raycastMask.value == 0)
        {
            raycastMask = -1;
        }
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

       

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
       

        if (raycastCamera == null)
        {
            Debug.LogWarning("[InputHandler] Raycast용 Camera가 없습니다.");
            return;
        }
        
        LayerMask unitMask = playerMask | enemyMask;

        var ray = raycastCamera.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit1, maxRayDistance))
        {
            Debug.Log($"무마스크 히트: {hit1.collider.name}");
            Debug.Log($"히트 레이어: {LayerMask.LayerToName(hit1.collider.gameObject.layer)}");
            Debug.Log($"레이어 번호: {hit1.collider.gameObject.layer}");
            Debug.Log($"raycastMask 값: {raycastMask.value}");
        }


        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, raycastMask))
        {
            DeselectUnit(logDeselect: true);
            return;
        }

        int layer = hit.collider.gameObject.layer;
        if (!IsLayerInMask(layer, unitMask))
        {
            DeselectUnit(logDeselect: true);
            return;
        }

        if (!TryGetUnitComponents(hit.collider, out Outline outline))
        {
            return;
        }

        // Player 선택 (독립)
        if (IsLayerInMask(layer, playerMask))
        {
            if (selectedPlayerOutline == outline)
            {
                outline.OutlineMode = Outline.Mode.OutlineVisible;
                return;
            }

            if (selectedPlayerOutline != null)
            {
                selectedPlayerOutline.OutlineMode = Outline.Mode.OutlineHidden;
            }

            selectedPlayerOutline = outline;
            selectedPlayerOutline.OutlineMode = Outline.Mode.OutlineVisible;
            return;
        }

        // Enemy 선택 (독립)
        if (IsLayerInMask(layer, enemyMask))
        {
            if (selectedEnemyOutline == outline)
            {
                outline.OutlineMode = Outline.Mode.OutlineVisible;
                return;
            }

            if (selectedEnemyOutline != null)
            {
                selectedEnemyOutline.OutlineMode = Outline.Mode.OutlineHidden;
            }

            selectedEnemyOutline = outline;
            selectedEnemyOutline.OutlineMode = Outline.Mode.OutlineVisible;
            return;
        }

        //LogSelection(layer, unit.UnitID);
    }

    private void OnDisable()
    {
        DeselectUnit(logDeselect: false);
    }

    /// <param name="logDeselect">빈 공간/비유닛 클릭 등으로 선택이 해제될 때만 로그</param>
    private void DeselectUnit(bool logDeselect)
    {
        bool hadSelection = selectedPlayerOutline != null || selectedEnemyOutline != null;

        if (selectedPlayerOutline != null)
        {
            selectedPlayerOutline.OutlineMode = Outline.Mode.OutlineHidden;
            selectedPlayerOutline = null;
        }

        if (selectedEnemyOutline != null)
        {
            selectedEnemyOutline.OutlineMode = Outline.Mode.OutlineHidden;
            selectedEnemyOutline = null;
        }

        if (logDeselect && hadSelection)
        {
            Debug.Log("선택 해제됨");
        }
    }

    // 추후에 데이터 사용 시에 함수 매개변수 추가하기
    // out BattleCharactorUnit unit
    private static bool TryGetUnitComponents(Collider collider, out Outline outline)
    {
        outline = collider.GetComponent<Outline>() ?? collider.GetComponentInParent<Outline>();
        //unit = collider.GetComponent<BattleCharactorUnit>() ?? collider.GetComponentInParent<BattleCharactorUnit>();

        if (outline == null)
        {
            Debug.LogWarning("[InputHandler] Outline을 찾을 수 없습니다: " + collider.name);
            //unit = null;
            return false;
        }

        //if (unit == null)
        //{
        //    Debug.LogWarning("[InputHandler] BattleCharactorUnit이 없는 유닛입니다: " + collider.name);
        //    outline = null;
        //    return false;
        //}

        return true;
    }

    private static bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void LogSelection(int gameObjectLayer, string unitId)
    {
        if (IsLayerInMask(gameObjectLayer, playerMask))
        {
            Debug.Log($"플레이어 선택됨 — ID: {unitId}");
            return;
        }

        if (IsLayerInMask(gameObjectLayer, enemyMask))
        {
            Debug.Log($"적 선택됨 — ID: {unitId}");
            return;
        }

        Debug.Log($"유닛 선택됨 (기타 레이어) — ID: {unitId}");
    }
}

/// <summary>
/// 전투 유닛 프리팹에 부착. UnitID로 식별합니다.
/// </summary>
[DisallowMultipleComponent]
public class BattleCharactorUnit : MonoBehaviour
{
    [SerializeField] private string unitID;

    /// <summary>비어 있으면 <see cref="CharactorScript"/>의 UnitData.Index를 사용합니다.</summary>
    public string UnitID
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(unitID))
            {
                return unitID;
            }

            var cs = GetComponent<CharactorScript>();
            if (cs != null && cs.Data != null && !string.IsNullOrWhiteSpace(cs.Data.Index))
            {
                return cs.Data.Index;
            }

            var es = GetComponent<EnemyScript>();
            if (es != null && es.Data != null && !string.IsNullOrWhiteSpace(es.Data.Index))
            {
                return es.Data.Index;
            }

            return string.Empty;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(unitID))
        {
            var cs = GetComponent<CharactorScript>();
            if (cs != null && cs.Data != null && !string.IsNullOrWhiteSpace(cs.Data.Index))
            {
                unitID = cs.Data.Index;
            }
        }
    }
#endif
}
