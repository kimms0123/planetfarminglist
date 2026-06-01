using UnityEngine;
using System;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance;

    [Header("시작 금액")]
    public int startingMoney = 0;

    private int currentMoney;

    public int CurrentMoney => currentMoney;

    public event Action<int> OnMoneyChanged;

    void Awake()
    {
        // 씬 전환에도 유지 + 중복 방지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentMoney = startingMoney;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"돈 획득! +{amount}G (보유: {currentMoney}G)");
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return false;
        if (currentMoney < amount)
        {
            Debug.Log("돈이 부족해요!");
            return false;
        }
        currentMoney -= amount;
        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"돈 사용! -{amount}G (보유: {currentMoney}G)");
        return true;
    }

    public bool HasMoney(int amount) => currentMoney >= amount;
}