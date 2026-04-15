using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Orora.TestMerge
{
    public class TMMinePanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text resourceTypeText;
        [SerializeField] private TMP_Text productionText;
        [SerializeField] private Button okButton;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        private void Awake()
        {
            if (okButton != null) okButton.onClick.AddListener(HidePanel);
            HidePanel();
        }

        private void OnEnable() { TMMine.MineClaimed += HandleMineClaimed; }
        private void OnDisable() { TMMine.MineClaimed -= HandleMineClaimed; }

        private void OnDestroy()
        {
            if (okButton != null) okButton.onClick.RemoveListener(HidePanel);
        }

        private void Update()
        {
            if (!IsPanelVisible()) return;
            if (Input.GetKeyDown(closeKey)) HidePanel();
        }

        private void HandleMineClaimed(TMMine mine)
        {
            if (mine == null) return;
            if (resourceTypeText != null) resourceTypeText.text = $"Resource : {mine.ProducedResource}";
            if (productionText != null)   productionText.text   = $"Per Turn : {mine.AmountPerTurn}";
            ShowPanel();
        }

        private void ShowPanel() { if (panelRoot != null) panelRoot.SetActive(true); }
        private void HidePanel() { if (panelRoot != null) panelRoot.SetActive(false); }
        private bool IsPanelVisible() { return panelRoot != null && panelRoot.activeSelf; }
    }
}
