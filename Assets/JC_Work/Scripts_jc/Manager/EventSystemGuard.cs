using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemGuard : MonoBehaviour
{
    private void Awake()
    {
        // 씬에 이미 다른 EventSystem이 있으면 자신을 비활성화
        var allEventSystems = FindObjectsOfType<EventSystem>();
        foreach (var es in allEventSystems)
        {
            if (es != GetComponent<EventSystem>() && es.gameObject != gameObject)
            {
                gameObject.SetActive(false);
                return;
            }
        }
    }

    private void OnEnable()
    {
        // 씬 전환 후 다시 활성화 시 체크
        var allEventSystems = FindObjectsOfType<EventSystem>();
        int activeCount = 0;
        foreach (var es in allEventSystems)
        {
            if (es.gameObject.activeInHierarchy) activeCount++;
        }
        if (activeCount > 1)
        {
            gameObject.SetActive(false);
        }
    }
}
