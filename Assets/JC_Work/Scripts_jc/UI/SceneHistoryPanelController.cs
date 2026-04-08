using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneHistoryPanelController : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI historyText;
    [SerializeField] private RectTransform titleBar;
    [SerializeField] private RectTransform resizeHandle;

    private SceneLoader sceneLoader;

    public void Setup(SceneLoader loader)
    {
        sceneLoader = loader;

        // 저장된 크기/위치 적용
        var rect = GetComponent<RectTransform>();
        rect.sizeDelta = sceneLoader.HistoryPanelSize;
        rect.anchoredPosition = sceneLoader.HistoryPanelPosition;

        // 드래그 이동 (타이틀 바)
        if (titleBar != null)
        {
            var drag = titleBar.gameObject.AddComponent<PanelDragHandler>();
            drag.SetTarget(rect, pos => sceneLoader.HistoryPanelPosition = pos);
        }

        // 리사이즈 (우하단 핸들)
        if (resizeHandle != null)
        {
            var resizer = resizeHandle.gameObject.AddComponent<PanelResizeHandler>();
            resizer.SetTarget(rect, new Vector2(150f, 100f), new Vector2(600f, 800f),
                size => sceneLoader.HistoryPanelSize = size);
        }

        RefreshHistoryDisplay();
    }

    public void RefreshHistoryDisplay()
    {
        if (historyText == null || sceneLoader == null) return;

        var history = sceneLoader.History;
        int currentIdx = sceneLoader.CurrentIndex;

        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < history.Count; i++)
        {
            string sceneName = history[i];
            string displayName = sceneLoader.GetDisplayName(sceneName);
            string marker = (i == currentIdx) ? " <<" : "";

            if (i == currentIdx)
            {
                sb.AppendLine($"<color=#FF4444>[{i}] {displayName}{marker}</color>");
            }
            else
            {
                sb.AppendLine($"[{i}] {displayName}{marker}");
            }
        }

        historyText.text = sb.ToString();
        historyText.ForceMeshUpdate();

        float preferredHeight = historyText.preferredHeight;
        scrollRect.content.sizeDelta = new Vector2(0f, preferredHeight);

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }
}
