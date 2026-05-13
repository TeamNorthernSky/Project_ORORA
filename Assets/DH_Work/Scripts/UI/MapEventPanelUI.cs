using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapEventPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text eventPromptText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private MapEventObject currentMapEvent;
    private ResourceManager resourceManager;

    private void Awake()
    {
        resourceManager = FindFirstObjectByType<ResourceManager>();

        if (yesButton != null)
            yesButton.onClick.AddListener(OnOkButtonClicked);

        if (noButton != null)
            noButton.onClick.AddListener(OnNoButtonClicked);

        HidePanel();
    }

    private void OnEnable()
    {
        MapEventObject.EventInteracted += HandleEventInteracted;
    }

    private void OnDisable()
    {
        MapEventObject.EventInteracted -= HandleEventInteracted;
    }

    private void OnDestroy()
    {
        if (yesButton != null)
            yesButton.onClick.RemoveListener(OnOkButtonClicked);

        if (noButton != null)
            noButton.onClick.RemoveListener(OnNoButtonClicked);
    }

    private void HandleEventInteracted(MapEventObject mapEvent)
    {
        if (mapEvent == null)
            return;

        currentMapEvent = mapEvent;

        if (eventPromptText != null)
            eventPromptText.text = $"Will you try this {mapEvent.EventKey}?";

        if (costText != null)
            costText.text = $"- {mapEvent.RequireAmount} {mapEvent.RequireResource}";

        if (yesButton != null)
        {
            if (resourceManager != null)
            {
                yesButton.interactable = resourceManager.HasResource(mapEvent.RequireResource, mapEvent.RequireAmount);
            }
            else
            {
                yesButton.interactable = false;
            }
        }

        ShowPanel();
    }

    private void OnOkButtonClicked()
    {
        if (currentMapEvent != null && resourceManager != null)
        {
            bool success = currentMapEvent.TryExecuteEvent(resourceManager);
            if (success)
            {
                HidePanel();
            }
        }
    }

    private void OnNoButtonClicked()
    {
        HidePanel();
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

        currentMapEvent = null;
    }
}
