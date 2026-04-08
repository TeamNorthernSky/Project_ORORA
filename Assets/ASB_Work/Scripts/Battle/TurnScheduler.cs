using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 전투 턴 순서 제어기.
/// - Speed 내림차순
/// - Speed가 같으면 플레이어(IsPlayer) 우선
/// - 죽은 유닛(IsDead)은 스킵
/// - 한 라운드(큐 소진)마다 다시 정렬하여 큐를 채움
/// </summary>
[DisallowMultipleComponent]
public class TurnScheduler : MonoBehaviour
{
    private readonly List<BattleCharactor> participants = new List<BattleCharactor>();
    private Queue<BattleCharactor> turnQueue = new Queue<BattleCharactor>();

    public BattleCharactor CurrentUnit { get; private set; }

    public void Initialize(List<BattleCharactor> units)
    {
        participants.Clear();
        if (units != null)
        {
            participants.AddRange(units.Where(u => u != null));
        }

        RebuildQueue();
        CurrentUnit = null;

        Debug.Log($"[TurnScheduler] 초기화 완료. 참여자={participants.Count}, 큐={turnQueue.Count}");
    }

    public BattleCharactor GetNextUnit()
    {
        // 무한 루프 방지: 참여자 수 만큼만 스킵 허용
        int safety = Mathf.Max(1, participants.Count) + 1;

        while (safety-- > 0)
        {
            if (turnQueue == null || turnQueue.Count == 0)
            {
                RebuildQueue();
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

        RebuildQueue();
        Debug.Log($"[TurnScheduler] 유닛 제거. 남은 참여자={participants.Count}, 큐={turnQueue.Count}");
    }

    private void RebuildQueue()
    {
        var ordered = participants
            .Where(u => u != null && !u.IsDead)
            .OrderByDescending(u => u.FinalStats.Speed)
            .ThenByDescending(u => u.IsPlayer)
            .ToList();

        turnQueue = new Queue<BattleCharactor>(ordered);
    }
}
