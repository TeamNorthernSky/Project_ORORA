using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 전투 전체 흐름 제어기.
/// - 참가자 관리 + 속도 기반 턴 정렬
/// - 코루틴 기반 무한 전투 루프
/// - 입력(더블 클릭) 연동 대기
/// </summary>
[DisallowMultipleComponent]
public class BattleFlowManager : MonoBehaviour
{
    [Header("Turn/Loop")]
    [SerializeField] private bool autoStartOnInitialize = true;
    [SerializeField] private float enemyThinkSeconds = 3f;
    [SerializeField] private float doubleClickWindow = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    [Header("Runtime lookup")]
    [Tooltip("Outline 등록 시 비활성 유닛 루트(CharactorScript/EnemyScript)도 FindObjects에 포함할지 여부")]
    [SerializeField] private bool includeInactiveUnitRootsInOutlineLookup = true;
    [SerializeField] private BattleManager battleManager;

    private readonly List<BattleCharactor> participants = new List<BattleCharactor>();
    private Queue<BattleCharactor> turnQueue = new Queue<BattleCharactor>();

    private readonly Dictionary<string, BattleCharactor> battleById = new Dictionary<string, BattleCharactor>();
    private readonly Dictionary<BattleCharactor, Outline> outlineByBattle = new Dictionary<BattleCharactor, Outline>();

    private Coroutine battleLoopRoutine;
    private int roundIndex = 0;

    /// <summary>직전 클릭이 유효한 적 전투체였을 때만 유지(더블클릭 1차 입력).</summary>
    private BattleCharactor lastClickedEnemyBattle;
    private float lastEnemyClickTime = -999f;
    private bool enemyDoubleClickTriggered;
    private BattleCharactor clickedEnemyBattle;

    public BattleCharactor CurrentUnit { get; private set; }

    private void OnEnable()
    {
        InputHandler.UnitClicked += HandleUnitClicked;
        InputHandler.EnemyDoubleClickChainInterrupted += OnEnemyDoubleClickChainInterrupted;
    }

    private void OnDisable()
    {
        InputHandler.UnitClicked -= HandleUnitClicked;
        InputHandler.EnemyDoubleClickChainInterrupted -= OnEnemyDoubleClickChainInterrupted;
        if (battleLoopRoutine != null)
        {
            StopCoroutine(battleLoopRoutine);
            battleLoopRoutine = null;
        }
    }

    public void Initialize(List<BattleCharactor> initialParticipants)
    {
        participants.Clear();
        if (initialParticipants != null)
        {
            participants.AddRange(initialParticipants.Where(u => u != null));
        }

        RebuildRuntimeLookup();
        RefreshQueue();
        CurrentUnit = null;
        roundIndex = 0;

        Log($"[BattleFlow] Initialize 완료. participants={participants.Count}, queue={turnQueue.Count}");

        if (autoStartOnInitialize)
        {
            StartBattleLoop();
        }
    }

    public void StartBattleLoop()
    {
        if (battleLoopRoutine != null)
        {
            StopCoroutine(battleLoopRoutine);
        }

        battleLoopRoutine = StartCoroutine(BattleLoop());
        Log("[BattleFlow] BattleLoop 시작");
    }

    public void StopBattleLoop()
    {
        if (battleLoopRoutine != null)
        {
            StopCoroutine(battleLoopRoutine);
            battleLoopRoutine = null;
            Log("[BattleFlow] BattleLoop 중지");
        }
    }

    public void RefreshQueue()
    {
        var ordered = participants
            .Where(u => u != null && !u.IsDead)
            .OrderByDescending(u => u.FinalStats.Speed)
            .ThenByDescending(u => u.IsPlayer)
            .ToList();

        turnQueue = new Queue<BattleCharactor>(ordered);
        roundIndex++;
        Log($"[BattleFlow] Round {roundIndex} 시작. queue={turnQueue.Count}");
    }

    public BattleCharactor GetNextUnit()
    {
        int safety = Mathf.Max(1, participants.Count) + 1;

        while (safety-- > 0)
        {
            if (turnQueue == null || turnQueue.Count == 0)
            {
                RefreshQueue();
                if (turnQueue.Count == 0)
                {
                    CurrentUnit = null;
                    return null;
                }
            }

            var next = turnQueue.Dequeue();
            if (next == null || next.IsDead)
            {
                continue;
            }

            CurrentUnit = next;
            return next;
        }

        CurrentUnit = null;
        return null;
    }

    public void RemoveUnit(BattleCharactor unit)
    {
        if (unit == null) return;

        participants.Remove(unit);
        if (CurrentUnit == unit)
        {
            CurrentUnit = null;
        }

        RefreshQueue();
        Log($"[BattleFlow] 유닛 제거: {GetUnitLabel(unit)}");
    }


    private IEnumerator BattleLoop()
    {
        while (true)
        {
            if (turnQueue == null || turnQueue.Count == 0)
            {
                RefreshQueue();
            }

            if (turnQueue == null || turnQueue.Count == 0)
            {
                Log("[BattleFlow] 남은 유닛이 없어 루프 대기");
                yield return null;
                continue;
            }

            var unit = turnQueue.Dequeue();
            if (unit == null || unit.IsDead)
            {
                continue;
            }

            CurrentUnit = unit;
            Log(FormatTurnStartLog(unit));
            SetOutline(unit, true);

            if (unit.IsPlayer)
            {
                Log("[BattleFlow] 플레이어 턴: 적 더블 클릭 대기");
                ResetDoubleClickState();
                
              
                while (!enemyDoubleClickTriggered)
                {
                    yield return null;
                }

                Log($"[BattleFlow] 더블 클릭 확인: target={GetUnitLabel(clickedEnemyBattle)}");
                yield return ExecuteAction(unit, clickedEnemyBattle);
            }
            else
            {
                Log($"[BattleFlow] 적 턴: {enemyThinkSeconds:0.0}s 대기");
                yield return new WaitForSeconds(enemyThinkSeconds);
                yield return ExecuteAction(unit, null);
            }

            SetOutline(unit, false);
            CurrentUnit = null;
            yield return null;
        }
    }

    /// <summary>
    /// 추후 애니메이션/데미지 계산 로직 주입 지점.
    /// </summary>
    protected virtual IEnumerator ExecuteAction(BattleCharactor unit, BattleCharactor target)
    {
        Log($"[BattleFlow] ExecuteAction: actor={GetUnitLabel(unit)}, target={GetUnitLabel(target)}");

        if (battleManager == null)
        {
            Debug.LogWarning("[BattleFlow] BattleManager가 할당되지 않아 행동 실행을 건너뜁니다.");
            yield return null;
            yield break;
        }

        if (CurrentUnit == null)
        {
            Debug.Log("Invalid target");
            yield return null;
            yield break;
        }

        // 현재 요구사항 범위: 플레이어 더블클릭 BasicAttack만 실행 연결.
        if (!CurrentUnit.IsPlayer)
        {
            yield return null;
            yield break;
        }

        // 플레이어 입력 액션은 더블 클릭 타겟 확정 후에만 들어온다.
        if (target == null ||
            target.IsDead ||
            target == CurrentUnit ||
            target.IsPlayer)
        {
            Debug.Log("Invalid target");
            yield return null;
            yield break;
        }

        var action = new BattleAction(
            CurrentUnit,
            target,
            BattleActionType.BasicAttack
        );

        battleManager.ExecuteAction(action);

        // TODO: 턴 종료 흐름 연결
        yield return null;
    }

    private void HandleUnitClicked(IUnitIdentifier clickedUnit)
    {
        if (enemyDoubleClickTriggered)
        {
            return;
        }

        if (clickedUnit == null || CurrentUnit == null || !CurrentUnit.IsPlayer)
        {
            return;
        }
        
        BattleCharactor clickedBattle = FindBattleByUnit(clickedUnit);

        if (clickedBattle == null || clickedBattle.IsPlayer || clickedBattle.IsDead)
        {
            lastClickedEnemyBattle = null;
            lastEnemyClickTime = -999f;
            return;
        }

        // GPT 피드백 반영: ID 중복 방지 및 unscaledTime 적용 — 동일 적은 BattleCharactor 참조로만 본다.
        float now = Time.unscaledTime;
        bool sameAsLastClick = lastClickedEnemyBattle != null && clickedBattle == lastClickedEnemyBattle;
        bool isDouble = sameAsLastClick && now - lastEnemyClickTime <= doubleClickWindow;

        lastClickedEnemyBattle = clickedBattle;
        lastEnemyClickTime = now;

        if (isDouble)
        {
            clickedEnemyBattle = clickedBattle;
            enemyDoubleClickTriggered = true;
        }
    }

    /// <summary>
    /// 빈 공간·비유닛 히트 등으로 선택이 끊긴 경우: 더블클릭 체인만 초기화.
    /// </summary>
    private void OnEnemyDoubleClickChainInterrupted()
    {
        if (CurrentUnit == null || !CurrentUnit.IsPlayer || enemyDoubleClickTriggered)
        {
            return;
        }

        lastClickedEnemyBattle = null;
        lastEnemyClickTime = -999f;
    }

    private void ResetDoubleClickState()
    {
        lastClickedEnemyBattle = null;
        lastEnemyClickTime = -999f;
        enemyDoubleClickTriggered = false;
        clickedEnemyBattle = null;
    }

    /// <summary>클릭한 IUnitIdentifier의 UnitID로 참가 BattleCharactor를 찾는다. 실패 시 verboseLog일 때만 경고.</summary>
    private BattleCharactor FindBattleByUnit(IUnitIdentifier unit)
    {
        if (unit == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(unit.UnitID))
        {
            if (verboseLog)
            {
                Debug.LogWarning(
                    "[BattleFlow] FindBattleByUnit 실패: UnitID가 비어 있습니다. (루트 스크립트에 UnitData 미주입 가능)");
            }

            return null;
        }

        if (!battleById.TryGetValue(unit.UnitID, out var battle))
        {
            if (verboseLog)
            {
                Debug.LogWarning(
                    $"[BattleFlow] FindBattleByUnit 실패: UnitID='{unit.UnitID}'에 해당하는 참가자가 없습니다. (battleById 키 불일치 가능)");
            }

            return null;
        }

        return battle;
    }

    /// <summary>
    /// 참가자 ID 맵을 만든 뒤, 씬의 CharactorScript/EnemyScript 루트만 명시적으로 수집해 Outline을 캐싱한다.
    /// </summary>
    private void RebuildRuntimeLookup()
    {
        battleById.Clear();
        outlineByBattle.Clear();

        foreach (var battle in participants)
        {
            if (battle == null || battle.UnitData == null) continue;
            string key = battle.UnitData.Index != null ? battle.UnitData.Index.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (!battleById.ContainsKey(key))
            {
                battleById.Add(key, battle);
            }
        }

        var inactiveMode = includeInactiveUnitRootsInOutlineLookup
            ? FindObjectsInactive.Include
            : FindObjectsInactive.Exclude;

        foreach (var cs in FindObjectsByType<CharactorScript>(inactiveMode, FindObjectsSortMode.None))
        {
            TryRegisterOutline(cs);
        }

        foreach (var es in FindObjectsByType<EnemyScript>(inactiveMode, FindObjectsSortMode.None))
        {
            TryRegisterOutline(es);
        }

        Log($"[BattleFlow] Lookup 재구성: id={battleById.Count}, outline={outlineByBattle.Count}");
    }

    /// <summary>
    /// 유닛 루트(데이터 스크립트) 기준 하향 탐색으로만 Outline을 찾아 등록한다(타 유닛 Outline 오참조 방지).
    /// </summary>
    private void TryRegisterOutline(IUnitIdentifier id)
    {
        if (id == null || string.IsNullOrWhiteSpace(id.UnitID)) return;
        if (!battleById.TryGetValue(id.UnitID, out var battle)) return;

        var comp = id as Component;
        if (comp == null) return;

        if (outlineByBattle.ContainsKey(battle)) return;

        var allOutlines = comp.GetComponentsInChildren<Outline>(true);
        if (allOutlines == null || allOutlines.Length == 0)
        {
            Debug.LogWarning($"[BattleFlow] UnitID={id.UnitID} 유닛 루트 하위에서 Outline을 찾지 못했습니다.");
            return;
        }

        if (allOutlines.Length > 1)
        {
            Debug.LogWarning(
                $"[BattleFlow] UnitID={id.UnitID}에 Outline이 {allOutlines.Length}개 있습니다. 첫 번째만 사용합니다.");
        }

        Outline outline = comp.GetComponent<Outline>() ?? allOutlines[0];
        if (outline == null)
        {
            return;
        }

        outlineByBattle.Add(battle, outline);
        outline.OutlineMode = Outline.Mode.OutlineHidden;
    }

    private void SetOutline(BattleCharactor battle, bool visible)
    {
        if (battle == null) return;
        if (!outlineByBattle.TryGetValue(battle, out var outline) || outline == null) return;
        outline.OutlineMode = visible ? Outline.Mode.OutlineVisible : Outline.Mode.OutlineHidden;
    }

    private string GetUnitLabel(BattleCharactor unit)
    {
        if (unit == null || unit.UnitData == null) return "null";
        string side = unit.IsPlayer ? "Player" : "Enemy";
        return $"{side}:{unit.UnitData.Index}:{unit.UnitData.Name}";
    }

    private string FormatTurnStartLog(BattleCharactor unit)
    {
        if (unit == null || unit.UnitData == null)
        {
            return "[턴 시작] 유닛 정보 없음";
        }

        string side = unit.IsPlayer ? "플레이어 진영" : "적 진영";
        string name = string.IsNullOrWhiteSpace(unit.UnitData.Name) ? "Unknown" : unit.UnitData.Name;
        string id = string.IsNullOrWhiteSpace(unit.UnitData.Index) ? "Unknown" : unit.UnitData.Index;
        return $"[턴 시작] {side}: {name} (ID: {id})";
    }

    private void Log(string message)
    {
        if (verboseLog)
        {
            Debug.Log(message);
        }
    }
}
