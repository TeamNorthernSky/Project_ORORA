using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public CurrencyManager Currency { get; private set; }
    public SceneLoader SceneLoader { get; private set; }

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    public DebugManager Debug { get; private set; }
    #endif

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeManagers();
    }

    private void InitializeManagers()
    {
        Currency = GetComponentInChildren<CurrencyManager>();
        Currency.Initialize();

        SceneLoader = GetComponentInChildren<SceneLoader>();
        if (SceneLoader != null) SceneLoader.Initialize();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug = GetComponentInChildren<DebugManager>(true);
        if (Debug != null) Debug.Initialize();
        #endif
    }
}
