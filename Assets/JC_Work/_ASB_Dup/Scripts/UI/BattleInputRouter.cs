using UnityEngine;

/// <summary>
/// UI 버튼 등 외부 입력을 InputHandler로 릴레이하는 얇은 래퍼.
/// 키보드 1/2/3은 기존대로 InputHandler.Update()가 직접 처리하고, 버튼 입력은 이 라우터를 경유한다.
/// </summary>
[DisallowMultipleComponent]
public class BattleInputRouter : MonoBehaviour
{
    [SerializeField] private InputHandler inputHandler;

    public void PressAction(PendingActionType actionType)
    {
        if (inputHandler == null)
        {
            Debug.LogWarning("[BattleInputRouter] inputHandler가 할당되지 않았습니다.");
            return;
        }

        inputHandler.TriggerAction(actionType);
    }
}
