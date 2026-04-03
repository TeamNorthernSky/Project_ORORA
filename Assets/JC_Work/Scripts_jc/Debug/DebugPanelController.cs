using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugPanelController : MonoBehaviour
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD

    [Header("탭 버튼")]
    [SerializeField] private Button logTabButton;

    [Header("탭 패널")]
    [SerializeField] private GameObject logTab;

    [Header("로그")]
    [SerializeField] private ScrollRect logScrollRect;
    [SerializeField] private TextMeshProUGUI logContent;
    [SerializeField] private Button logFilterAll;
    [SerializeField] private Button logFilterLog;
    [SerializeField] private Button logFilterWarning;
    [SerializeField] private Button logFilterError;
    [SerializeField] private Button logClear;

    private List<LogEntry> logEntries = new List<LogEntry>();
    private DebugLogType currentFilter = DebugLogType.Log;
    private bool showAll = true;
    private int maxLogEntries = 200;

    private struct LogEntry
    {
        public string timestamp;
        public string message;
        public DebugLogType type;
    }

    public void Initialize()
    {
        gameObject.SetActive(false);
        BindLogButtons();
        BindTabButtons();
        Application.logMessageReceived += HandleUnityLog;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleUnityLog;
    }

    public void Toggle()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    // --- 탭 전환 ---

    private void BindTabButtons()
    {
        logTabButton.onClick.AddListener(() => SwitchTab(logTab));
        SwitchTab(logTab);
    }

    private void SwitchTab(GameObject activeTab)
    {
        logTab.SetActive(activeTab == logTab);
    }

    // --- 로그 ---

    private void BindLogButtons()
    {
        logFilterAll.onClick.AddListener(() => { showAll = true; RefreshLogDisplay(); });
        logFilterLog.onClick.AddListener(() => { showAll = false; currentFilter = DebugLogType.Log; RefreshLogDisplay(); });
        logFilterWarning.onClick.AddListener(() => { showAll = false; currentFilter = DebugLogType.Warning; RefreshLogDisplay(); });
        logFilterError.onClick.AddListener(() => { showAll = false; currentFilter = DebugLogType.Error; RefreshLogDisplay(); });
        logClear.onClick.AddListener(() => { logEntries.Clear(); RefreshLogDisplay(); });
    }

    private void HandleUnityLog(string condition, string stackTrace, LogType type)
    {
        if (this == null) return;

        DebugLogType debugType;
        switch (type)
        {
            case LogType.Warning:
                debugType = DebugLogType.Warning;
                break;
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                debugType = DebugLogType.Error;
                break;
            default:
                debugType = DebugLogType.Log;
                break;
        }

        AddLog(condition, debugType);
    }

    public void AddLog(string message, DebugLogType type)
    {
        var entry = new LogEntry
        {
            timestamp = DateTime.Now.ToString("HH:mm:ss"),
            message = message,
            type = type,
        };

        logEntries.Add(entry);

        if (logEntries.Count > maxLogEntries)
        {
            logEntries.RemoveAt(0);
        }

        if (gameObject.activeSelf)
        {
            RefreshLogDisplay();
        }
    }

    private void RefreshLogDisplay()
    {
        var sb = new System.Text.StringBuilder();

        foreach (var entry in logEntries)
        {
            if (!showAll && entry.type != currentFilter)
                continue;

            string colorTag;
            string typeTag;
            switch (entry.type)
            {
                case DebugLogType.Warning:
                    colorTag = "#FFFF00";
                    typeTag = "WAR";
                    break;
                case DebugLogType.Error:
                    colorTag = "#FF4444";
                    typeTag = "ERR";
                    break;
                default:
                    colorTag = "#000000";
                    typeTag = "LOG";
                    break;
            }

            sb.AppendLine($"<color={colorTag}>{entry.timestamp} [{typeTag}] {entry.message}</color>");
        }

        logContent.text = sb.ToString();

        Canvas.ForceUpdateCanvases();
        logScrollRect.verticalNormalizedPosition = 0f;
    }

    #endif
}
