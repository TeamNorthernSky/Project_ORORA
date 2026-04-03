using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public CurrencyManager Currency { get; private set; }

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    public DebugManager Debug { get; private set; }
    #endif

    [SerializeField] private string firstSceneName = "TestScene";

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

        if (SceneManager.GetActiveScene().name == "BootScene")
        {
            LoadFirstScene();
        }
    }

    private void InitializeManagers()
    {
        Currency = GetComponentInChildren<CurrencyManager>();
        Currency.Initialize();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug = GetComponentInChildren<DebugManager>(true);
        if (Debug != null) Debug.Initialize();
        #endif
    }

    private void LoadFirstScene()
    {
        Addressables.LoadSceneAsync(firstSceneName);
    }
}
