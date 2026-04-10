using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MinePanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text resourceTypeText;
    [SerializeField] private TMP_Text productionText;
    [SerializeField] private Button okButton;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private void Awake()
    {
        if (okButton != null)
            okButton.onClick.AddListener(HidePanel);

        HidePanel();
    }

    private void OnEnable()
    {
        Mine.MineClaimed += HandleMineClaimed;
    }

    private void OnDisable()
    {
        Mine.MineClaimed -= HandleMineClaimed;
    }

    private void OnDestroy()
    {
        if (okButton != null)
            okButton.onClick.RemoveListener(HidePanel);
    }

    private void Update()
    {
        if (!IsPanelVisible())
            return;

        if (Input.GetKeyDown(closeKey))
            HidePanel();
    }

    private void HandleMineClaimed(Mine mine)
    {
        if (mine == null)
            return;

        if (resourceTypeText != null)
            resourceTypeText.text = $"Resource : {mine.resourceType}";

        if (productionText != null)
            productionText.text = $"Per Turn : {mine.resourcePerTurn}";

        ShowPanel();
    }

    private void ShowPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    private void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private bool IsPanelVisible()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }
}
