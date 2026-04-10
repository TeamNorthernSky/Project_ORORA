using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 플레이어/적 유닛 클릭 선택. Quick Outline은 OutlineMode(Visible/Hidden)로 전환해 셰이더 유지.
/// </summary>
[DisallowMultipleComponent]
public class InputHandler : MonoBehaviour
{
    //클릭시 작동할 함수 저장소
    public static event Action<IUnitIdentifier> UnitClicked;

    /// <summary>
    /// 레이캐스트 실패·비유닛 레이어 등으로 유닛 선택 클릭으로 이어지지 않을 때(더블클릭 체인 초기화용).
    /// </summary>
    public static event Action EnemyDoubleClickChainInterrupted;

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



        //if (Physics.Raycast(ray, out RaycastHit hit1, maxRayDistance))
        //{

        //    // 디버그: 레이어 마스크가 제대로 적용되고 있는지 확인
        //    Debug.Log($"무마스크 히트: {hit1.collider.name}");
        //    Debug.Log($"히트 레이어: {LayerMask.LayerToName(hit1.collider.gameObject.layer)}");
        //    Debug.Log($"레이어 번호: {hit1.collider.gameObject.layer}");
        //    Debug.Log($"raycastMask 값: {raycastMask.value}");
        //}


        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, raycastMask))
        {
            DeselectUnit(logDeselect: true);
            EnemyDoubleClickChainInterrupted?.Invoke();
            return;
        }

        int layer = hit.collider.gameObject.layer;
        if (!IsLayerInMask(layer, unitMask))
        {
            DeselectUnit(logDeselect: true);
            EnemyDoubleClickChainInterrupted?.Invoke();
            return;
        }

        if (!TryGetUnitComponents(hit.collider, out Outline outline, out IUnitIdentifier unit))
        {
            EnemyDoubleClickChainInterrupted?.Invoke();
            return;
        }

        // Player 선택 (독립)
        if (IsLayerInMask(layer, playerMask))
        {
            if (selectedPlayerOutline == outline)
            {
                outline.OutlineMode = Outline.Mode.OutlineVisible;
                UnitClicked?.Invoke(unit);
                return;
            }

            if (selectedPlayerOutline != null)
            {
                selectedPlayerOutline.OutlineMode = Outline.Mode.OutlineHidden;
            }

            selectedPlayerOutline = outline;
            selectedPlayerOutline.OutlineMode = Outline.Mode.OutlineVisible;
            UnitClicked?.Invoke(unit);
            return;
        }

        // Enemy 선택 (독립)
        if (IsLayerInMask(layer, enemyMask))
        {
            if (selectedEnemyOutline == outline)
            {
                outline.OutlineMode = Outline.Mode.OutlineVisible;
                UnitClicked?.Invoke(unit);
                return;
            }

            if (selectedEnemyOutline != null)
            {
                selectedEnemyOutline.OutlineMode = Outline.Mode.OutlineHidden;
            }

            selectedEnemyOutline = outline;
            selectedEnemyOutline.OutlineMode = Outline.Mode.OutlineVisible;
            UnitClicked?.Invoke(unit);
            return;
        }

        EnemyDoubleClickChainInterrupted?.Invoke();
        LogSelection(layer, unit.UnitID);
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

    /// <summary>
    /// 클릭된 콜라이더 기준 상향 탐색: 가까운 Outline과 루트 식별자(IUnitIdentifier)를 찾고,
    /// 둘이 동일 유닛 서브트리(루트 데이터 오브젝트 하위)에 속하는지 검증한다.
    /// </summary>
    private static bool TryGetUnitComponents(
        Collider collider,
        out Outline outline,
        out IUnitIdentifier identifier)
    {
        outline = collider.GetComponent<Outline>() ?? collider.GetComponentInParent<Outline>();

        identifier = collider.GetComponentInParent<IUnitIdentifier>(true);
        if (identifier == null)
        {
            foreach (var mb in collider.GetComponentsInParent<MonoBehaviour>(true))
            {
                if (mb is IUnitIdentifier uid)
                {
                    identifier = uid;
                    break;
                }
            }
        }

        if (outline == null)
        {
            Debug.LogWarning("[InputHandler] Outline을 찾을 수 없습니다: " + collider.name);
            identifier = null;
            return false;
        }

        if (identifier == null)
        {
            Debug.LogWarning("[InputHandler] IUnitIdentifier가 없는 유닛입니다: " + collider.name);
            outline = null;
            return false;
        }

        var idComp = identifier as Component;
        if (idComp == null || !IsOutlineUnderUnitRoot(idComp, outline))
        {
            Debug.LogWarning(
                "[InputHandler] Outline과 IUnitIdentifier가 동일 유닛 계층에 있지 않습니다: " + collider.name);
            outline = null;
            identifier = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 루트(데이터 스크립트) 트랜스폼을 기준으로, Outline이 그 서브트리 안에만 있는지 확인한다.
    /// </summary>
    private static bool IsOutlineUnderUnitRoot(Component unitRoot, Outline outline)
    {
        if (unitRoot == null || outline == null)
        {
            return false;
        }

        Transform r = unitRoot.transform;
        Transform o = outline.transform;
        return o == r || o.IsChildOf(r);
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
