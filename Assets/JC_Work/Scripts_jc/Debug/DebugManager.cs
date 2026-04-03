using UnityEngine;

public class DebugManager : MonoBehaviour
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD

    public static DebugManager Instance { get; private set; }

    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;

    private DebugPanelController panelController;

    public void Initialize()
    {
        Instance = this;
        panelController = GetComponentInChildren<DebugPanelController>(true);
        if (panelController != null)
        {
            panelController.Initialize();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        if (panelController != null)
        {
            panelController.Toggle();
        }
    }

    #endif
}
