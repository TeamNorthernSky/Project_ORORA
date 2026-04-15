using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Orora.TestMerge
{
    public class TMTurnUI : MonoBehaviour
    {
        [SerializeField] private TMTurnManager turnManager;
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private Button endTurnButton;

        private void Awake()
        {
            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(HandleEndTurnClick);
        }

        private void OnDestroy()
        {
            if (endTurnButton != null)
                endTurnButton.onClick.RemoveListener(HandleEndTurnClick);
        }

        private void Update()
        {
            if (dayText != null && turnManager != null)
                dayText.text = "Day : " + turnManager.CurrentDay;
        }

        private void HandleEndTurnClick()
        {
            turnManager?.EndPlayerTurn();
        }
    }
}
