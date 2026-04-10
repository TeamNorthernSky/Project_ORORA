using UnityEngine;
using TMPro; // TextMeshPro 쓸 경우

public class ResourceUI : MonoBehaviour
{
    [SerializeField] private ResourceManager resourceManager;

    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text woodText;
    [SerializeField] private TMP_Text oreText;

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        goldText.text = "Gold : "+ resourceManager.GetAmount(ResourceType.Gold).ToString();
        woodText.text = "Wood : " + resourceManager.GetAmount(ResourceType.Wood).ToString();
        oreText.text = "Ore : " + resourceManager.GetAmount(ResourceType.Ore).ToString();
    }
}