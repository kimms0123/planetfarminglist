using UnityEngine;
using System;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance;

    [Header("½ÃÀÛ ±Ý¾×")]
    public int startingMoney = 0;

    private int currentMoney;

    public int CurrentMoney => currentMoney;

    public event Action<int> OnMoneyChanged;

    void Awake()
    {
        Instance = this;
        currentMoney = startingMoney;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"µ· È¹µæ! +{amount}G (º¸À¯: {currentMoney}G)");
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return false;
        if (currentMoney < amount)
        {
            Debug.Log("µ·ÀÌ ºÎÁ·ÇØ¿ä!");
            return false;
        }
        currentMoney -= amount;
        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"µ· »ç¿ë! -{amount}G (º¸À¯: {currentMoney}G)");
        return true;
    }

    public bool HasMoney(int amount) => currentMoney >= amount;
}