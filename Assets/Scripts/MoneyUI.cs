using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    public TextMeshProUGUI moneyText;

    void Start()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged += UpdateUI;
            UpdateUI(MoneyManager.Instance.CurrentMoney);
        }
    }

    void OnDestroy()
    {
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= UpdateUI;
    }

    void UpdateUI(int amount)
    {
        if (moneyText != null)
            moneyText.text = $"{amount:N0} G";
    }
}