using UnityEngine;
using TMPro;

namespace Orora.TestMerge
{
    public class TMResourceUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text woodText;
        [SerializeField] private TMP_Text oreText;

        private void Update()
        {
            if (goldText != null) goldText.text = "Gold : " + TMResourceBridge.Get(TMResourceType.Gold);
            if (woodText != null) woodText.text = "Wood : " + TMResourceBridge.Get(TMResourceType.Wood);
            if (oreText != null)  oreText.text  = "Ore : "  + TMResourceBridge.Get(TMResourceType.Ore);
        }
    }
}
