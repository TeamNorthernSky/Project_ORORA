using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestCanvasController : MonoBehaviour
{
    [Header("재화 표시")]
    [SerializeField] private TMP_InputField goldDisplay;
    [SerializeField] private TMP_InputField woodDisplay;

    [Header("골드 버튼")]
    [SerializeField] private Button goldAddButton;
    [SerializeField] private Button goldSubtractButton;
    [SerializeField] private Button goldResetButton;

    [Header("목재 입력")]
    [SerializeField] private TMP_InputField woodInput;
    [SerializeField] private Button woodAddButton;
    [SerializeField] private Button woodResetButton;

    [Header("전체")]
    [SerializeField] private Button resetAllButton;

    [Header("CanvasGroup 제어")]
    [SerializeField] private CanvasGroup canvasGroup;

    private Color normalColor = Color.black;
    private Color negativeColor = Color.red;

    private void Start()
    {
        SetupDisplayFields();
        BindButtons();
        BindWoodInput();
        SubscribeCurrencyEvents();
        RefreshAllDisplays();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.Currency != null)
        {
            GameManager.Instance.Currency.OnCurrencyChanged -= HandleCurrencyChanged;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleCanvasGroup();
        }
    }

    private void SetupDisplayFields()
    {
        goldDisplay.readOnly = true;
        woodDisplay.readOnly = true;
    }

    private void BindButtons()
    {
        goldAddButton.onClick.AddListener(() => GameManager.Instance.Currency.Add(CurrencyType.Gold, 100));
        goldSubtractButton.onClick.AddListener(() => GameManager.Instance.Currency.Add(CurrencyType.Gold, -100));
        goldResetButton.onClick.AddListener(() => GameManager.Instance.Currency.Reset(CurrencyType.Gold));

        woodAddButton.onClick.AddListener(OnWoodAddClicked);
        woodResetButton.onClick.AddListener(() => GameManager.Instance.Currency.Reset(CurrencyType.Wood));

        resetAllButton.onClick.AddListener(() => GameManager.Instance.Currency.ResetAll());
    }

    private void BindWoodInput()
    {
        woodInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        woodInput.text = "";
        woodAddButton.interactable = false;

        woodInput.onValueChanged.AddListener(OnWoodInputChanged);
    }

    private void OnWoodInputChanged(string value)
    {
        bool valid = int.TryParse(value, out int amount) && amount != 0;
        woodAddButton.interactable = valid;
    }

    private void OnWoodAddClicked()
    {
        if (int.TryParse(woodInput.text, out int amount) && amount != 0)
        {
            GameManager.Instance.Currency.Add(CurrencyType.Wood, amount);
            woodInput.text = "";
        }
        else
        {
            woodInput.text = "0";
        }
    }

    private void SubscribeCurrencyEvents()
    {
        GameManager.Instance.Currency.OnCurrencyChanged += HandleCurrencyChanged;
    }

    private void HandleCurrencyChanged(CurrencyType type, int newValue)
    {
        switch (type)
        {
            case CurrencyType.Gold:
                UpdateDisplay(goldDisplay, newValue, type);
                break;
            case CurrencyType.Wood:
                UpdateDisplay(woodDisplay, newValue, type);
                break;
        }
    }

    private void UpdateDisplay(TMP_InputField display, int value, CurrencyType type)
    {
        display.text = value.ToString();
        display.textComponent.color = GameManager.Instance.Currency.IsNegative(type) ? negativeColor : normalColor;
    }

    private void RefreshAllDisplays()
    {
        var currency = GameManager.Instance.Currency;
        UpdateDisplay(goldDisplay, currency.Get(CurrencyType.Gold), CurrencyType.Gold);
        UpdateDisplay(woodDisplay, currency.Get(CurrencyType.Wood), CurrencyType.Wood);
    }

    private void ToggleCanvasGroup()
    {
        bool isActive = canvasGroup.interactable;
        canvasGroup.interactable = !isActive;
        canvasGroup.alpha = isActive ? 0.3f : 1f;
    }
}
