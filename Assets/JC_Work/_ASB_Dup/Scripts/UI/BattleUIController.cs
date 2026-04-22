using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 UI 전체 제어:
/// - BattleFlowManager.CurrentUnit 폴링 → 턴 전환 감지 → 하단 로그 패널 클리어
/// - Application.logMessageReceived 훅 → 프리픽스 화이트리스트로 필터 → 해당 패널에 라우팅
/// - 우상단(적 행동 로그): 현재 턴 소유자가 적일 때만 적재. 적 턴 시작 시 클리어, 아군 턴에는 직전 내용 유지(A안).
/// - 하단(현재 턴 로그): 화이트리스트 전체. 매 턴 시작 직전 클리어.
/// 스크롤은 auto-scroll to bottom만 구현(프로토타입).
/// </summary>
[DisallowMultipleComponent]
public class BattleUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleFlowManager battleFlowManager;

    [Header("Enemy Action Log (우상단)")]
    [SerializeField] private TextMeshProUGUI enemyLogText;
    [SerializeField] private ScrollRect enemyLogScrollRect;

    [Header("Current Turn Log (하단)")]
    [SerializeField] private TextMeshProUGUI turnLogText;
    [SerializeField] private ScrollRect turnLogScrollRect;

    [Header("Filter")]
    [Tooltip("이 문자열을 포함한 로그만 UI에 표시.")]
    [SerializeField] private string[] whitelistPrefixes =
    {
        "[턴 시작]",
        "[BattleFlow]",
        "[Battle]",
        "[Skill/"
    };

    [Tooltip("프리픽스 없는 한글 전투 로그 캡처용 키워드.")]
    [SerializeField] private string[] whitelistKeywords =
    {
        "피해를 입혔습니다",
        "체력을 회복",
        "리타이어"
    };

    private readonly StringBuilder enemyBuf = new StringBuilder(1024);
    private readonly StringBuilder turnBuf = new StringBuilder(1024);

    private BattleCharactor lastCurrentUnit;

    private void OnEnable()
    {
        Application.logMessageReceived += OnLogReceived;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnLogReceived;
    }

    private void Update()
    {
        BattleCharactor current = battleFlowManager != null ? battleFlowManager.CurrentUnit : null;
        if (current != lastCurrentUnit)
        {
            if (current != null)
            {
                OnTurnOwnerChanged(current);
            }
            lastCurrentUnit = current;
        }
    }

    private void OnTurnOwnerChanged(BattleCharactor next)
    {
        // 턴 시작: 하단 패널은 매번 클리어
        turnBuf.Clear();
        ApplyTurnBuffer();

        // 우상단은 새 소유자가 적일 때만 클리어 (아군 턴이면 직전 적 행동 유지)
        if (!next.IsPlayer)
        {
            enemyBuf.Clear();
            ApplyEnemyBuffer();
        }
    }

    private void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        if (string.IsNullOrEmpty(condition))
        {
            return;
        }

        if (!PassesWhitelist(condition))
        {
            return;
        }

        AppendLine(turnBuf, condition);
        ApplyTurnBuffer();

        BattleCharactor current = battleFlowManager != null ? battleFlowManager.CurrentUnit : null;
        if (current != null && !current.IsPlayer)
        {
            AppendLine(enemyBuf, condition);
            ApplyEnemyBuffer();
        }
    }

    private static void AppendLine(StringBuilder sb, string line)
    {
        if (sb.Length > 0)
        {
            sb.Append('\n');
        }
        sb.Append(line);
    }

    private bool PassesWhitelist(string message)
    {
        for (int i = 0; i < whitelistPrefixes.Length; i++)
        {
            string prefix = whitelistPrefixes[i];
            if (!string.IsNullOrEmpty(prefix) && message.Contains(prefix))
            {
                return true;
            }
        }
        for (int i = 0; i < whitelistKeywords.Length; i++)
        {
            string keyword = whitelistKeywords[i];
            if (!string.IsNullOrEmpty(keyword) && message.Contains(keyword))
            {
                return true;
            }
        }
        return false;
    }

    private void ApplyTurnBuffer()
    {
        if (turnLogText != null)
        {
            turnLogText.text = turnBuf.ToString();
        }
        ScrollToBottom(turnLogScrollRect);
    }

    private void ApplyEnemyBuffer()
    {
        if (enemyLogText != null)
        {
            enemyLogText.text = enemyBuf.ToString();
        }
        ScrollToBottom(enemyLogScrollRect);
    }

    private void ScrollToBottom(ScrollRect rect)
    {
        if (rect == null)
        {
            return;
        }
        // 0f = bottom, 1f = top
        rect.verticalNormalizedPosition = 0f;
    }
}
