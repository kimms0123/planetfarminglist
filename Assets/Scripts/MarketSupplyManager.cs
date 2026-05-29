using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 시장 공급량 매니저
/// - 아이템별 공급량 추적 (판매할 때마다 증가)
/// - 시간이 지나면 공급량 자동 감소 (수요 회복)
/// - 공급량에 따라 가격 페널티 적용
/// 보고서 9.2 (공급-수요 시뮬레이션) 참고
/// </summary>
public class MarketSupplyManager : MonoBehaviour
{
    public static MarketSupplyManager Instance;

    [Header("공급 페널티 파라미터")]
    [SerializeField] private float decayConstant = 5f;       // 페널티 곡선 강도 (작을수록 빨리 하락)
    [SerializeField] private float minPenalty = 0.1f;         // 최저 페널티 (10%까지 떨어짐)

    [Header("시간 회복 파라미터")]
    [SerializeField] private float decayPerSecond = 0.02f;    // 초당 공급량 감소량
    [SerializeField] private float decayStartDelay = 5f;      // 마지막 판매 후 회복 시작까지 대기 시간 (초)

    [Header("디버그")]
    [SerializeField] private bool logEachUpdate = false;

    // 아이템별 공급량 (key: ItemData, value: 누적 공급량)
    private Dictionary<ItemData, float> supplyMap = new Dictionary<ItemData, float>();
    // 마지막 판매 시간 (회복 지연용)
    private Dictionary<ItemData, float> lastSoldTime = new Dictionary<ItemData, float>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // 시간 흐름에 따라 모든 아이템 공급량 자동 감소
        DecaySupplyOverTime();
    }

    // ─────────────────────────────────────────────
    // 외부 호출 - 판매 시 공급량 증가
    // ─────────────────────────────────────────────
    public void RegisterSale(ItemData item, int quantity)
    {
        if (item == null) return;

        if (!supplyMap.ContainsKey(item))
            supplyMap[item] = 0f;

        supplyMap[item] += quantity;
        lastSoldTime[item] = Time.time;

        Debug.Log($"[Market] {item.itemName} 공급 +{quantity} → 현재 공급량 {supplyMap[item]:F1}");
    }

    // ─────────────────────────────────────────────
    // 공급 페널티 조회 (가격에 곱할 배율)
    // 0.1 ~ 1.0 사이 값
    // ─────────────────────────────────────────────
    public float GetSupplyMultiplier(ItemData item)
    {
        if (item == null) return 1.0f;
        if (!supplyMap.ContainsKey(item) || supplyMap[item] <= 0f) return 1.0f;

        float supply = supplyMap[item];
        float multiplier = Mathf.Exp(-supply / decayConstant);
        return Mathf.Max(multiplier, minPenalty);
    }

    public float GetSupplyAmount(ItemData item)
    {
        if (item == null || !supplyMap.ContainsKey(item)) return 0f;
        return supplyMap[item];
    }

    // ─────────────────────────────────────────────
    // 시간 회복 - Update에서 호출
    // ─────────────────────────────────────────────
    private void DecaySupplyOverTime()
    {
        if (supplyMap.Count == 0) return;

        float decay = decayPerSecond * Time.deltaTime;
        var keys = new List<ItemData>(supplyMap.Keys);

        foreach (var item in keys)
        {
            if (supplyMap[item] <= 0f) continue;

            // 마지막 판매 후 일정 시간 지난 후 회복 시작
            if (lastSoldTime.ContainsKey(item))
            {
                float elapsed = Time.time - lastSoldTime[item];
                if (elapsed < decayStartDelay) continue;
            }

            supplyMap[item] = Mathf.Max(0f, supplyMap[item] - decay);

            if (logEachUpdate)
                Debug.Log($"[Market] {item.itemName} 회복 중 → {supplyMap[item]:F2}");
        }
    }

    // ─────────────────────────────────────────────
    // 디버그용
    // ─────────────────────────────────────────────
    public string GetDebugInfo()
    {
        if (supplyMap.Count == 0) return "공급 데이터 없음";

        var sb = new System.Text.StringBuilder();
        foreach (var kvp in supplyMap)
        {
            if (kvp.Value <= 0.1f) continue;
            float mult = GetSupplyMultiplier(kvp.Key);
            sb.AppendLine($"  {kvp.Key.itemName}: 공급 {kvp.Value:F1} → 배율 x{mult:F2}");
        }
        return sb.Length > 0 ? sb.ToString().TrimEnd() : "공급 데이터 없음";
    }

    // 디버그용 리셋
    public void ResetAll()
    {
        supplyMap.Clear();
        lastSoldTime.Clear();
        Debug.Log("[Market] 모든 공급량 리셋");
    }
}