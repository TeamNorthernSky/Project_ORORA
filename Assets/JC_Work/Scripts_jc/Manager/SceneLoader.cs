using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 순환 순서: 타이틀 → 로비 → 플레이 → 엔딩 → 타이틀
    public static readonly string[] SceneOrder =
        { "TitleScene", "LobbyScene", "PlayScene", "EndingScene" };

    private static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>
    {
        { "TitleScene", "타이틀 씬" },
        { "LobbyScene", "로비 씬" },
        { "PlayScene", "플레이 씬" },
        { "EndingScene", "엔딩 씬" },
        { "TestScene", "테스트 씬" },
        { "BootScene", "부트 씬" }
    };

    // 이력: List + Index (브라우저 뒤로/앞으로 패턴)
    private List<string> history = new List<string>();
    private int currentIndex = -1;
    private bool isLoading;

    public string CurrentSceneName { get; private set; }

    // 이력 패널 크기/위치 (씬 전환 시에도 유지)
    public Vector2 HistoryPanelSize { get; set; } = new Vector2(280f, 300f);
    public Vector2 HistoryPanelPosition { get; set; } = new Vector2(1550f, -60f);

    public void Initialize()
    {
        CurrentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CurrentSceneName = scene.name;

        // 최초 씬이 이력에 없으면 추가
        if (history.Count == 0 && IsNavigationScene(scene.name))
        {
            history.Add(scene.name);
            currentIndex = 0;
        }

        if (IsNavigationScene(scene.name))
        {
            StartCoroutine(SpawnNavigationCanvas());
        }
    }

    // --- 씬 전환: 이력에 새 항목 추가 ---

    public void LoadScene(string sceneName)
    {
        if (isLoading || sceneName == CurrentSceneName) return;

        // currentIndex 이후의 앞쪽 이력 삭제 (브라우저와 동일)
        if (currentIndex < history.Count - 1)
        {
            history.RemoveRange(currentIndex + 1, history.Count - currentIndex - 1);
        }

        history.Add(sceneName);
        currentIndex = history.Count - 1;

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    // --- 다음 씬 ---

    public void LoadNextScene()
    {
        if (isLoading) return;

        if (HasForwardHistory())
        {
            // 앞쪽 이력이 있으면 이력에서 복원
            currentIndex++;
            StartCoroutine(LoadSceneRoutine(history[currentIndex]));
        }
        else
        {
            // 최상단이면 순환 다음 씬을 이력에 추가
            string next = GetNextInOrder(CurrentSceneName);
            if (next != null) LoadScene(next);
        }
    }

    // --- 이전 씬 ---

    public void LoadPreviousScene()
    {
        if (isLoading) return;

        if (HasBackHistory())
        {
            // 뒤쪽 이력이 있으면 이력에서 복원
            currentIndex--;
            StartCoroutine(LoadSceneRoutine(history[currentIndex]));
        }
        else
        {
            // 최하단이면 순환 이전 씬을 이력 앞에 삽입
            string prev = GetPreviousInOrder(CurrentSceneName);
            if (prev != null)
            {
                history.Insert(0, prev);
                // currentIndex는 0 유지 (삽입으로 기존 항목이 밀림)
                StartCoroutine(LoadSceneRoutine(prev));
            }
        }
    }

    // --- 이력 조회 ---

    public IReadOnlyList<string> History => history;
    public int CurrentIndex => currentIndex;

    public bool HasBackHistory()
    {
        return currentIndex > 0;
    }

    public bool HasForwardHistory()
    {
        return currentIndex < history.Count - 1;
    }

    // --- 순환 순서 조회 ---

    public string GetNextInOrder(string sceneName)
    {
        int index = System.Array.IndexOf(SceneOrder, sceneName);
        if (index < 0) return null;
        return SceneOrder[(index + 1) % SceneOrder.Length];
    }

    public string GetPreviousInOrder(string sceneName)
    {
        int index = System.Array.IndexOf(SceneOrder, sceneName);
        if (index < 0) return null;
        return SceneOrder[(index - 1 + SceneOrder.Length) % SceneOrder.Length];
    }

    public string[] GetDirectJumpTargets(string currentScene)
    {
        var targets = new List<string>();
        foreach (var scene in SceneOrder)
        {
            if (scene != currentScene)
                targets.Add(scene);
        }
        return targets.ToArray();
    }

    public string GetDisplayName(string sceneName)
    {
        return DisplayNames.TryGetValue(sceneName, out string name) ? name : sceneName;
    }

    // --- 내부 ---

    private bool IsNavigationScene(string sceneName)
    {
        return System.Array.IndexOf(SceneOrder, sceneName) >= 0;
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        var handle = Addressables.LoadSceneAsync(sceneName);
        while (!handle.IsDone)
            yield return null;

        isLoading = false;
    }

    private IEnumerator SpawnNavigationCanvas()
    {
        var handle = Addressables.LoadAssetAsync<GameObject>("SceneNavigationCanvas");
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var canvas = Instantiate(handle.Result);
            canvas.name = "SceneNavigationCanvas";

            var controller = canvas.GetComponent<SceneNavigationController>();
            if (controller == null)
                controller = canvas.AddComponent<SceneNavigationController>();

            controller.Setup(this);
        }
        else
        {
            Debug.LogError("[SceneLoader] SceneNavigationCanvas 프리팹 로드 실패");
        }
    }
}
