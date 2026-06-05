using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 계절별 작물 수요 적합도 제공자 (보고서 5.2: 계절 포함 · 날씨 제외)
///
/// RBFN 입력 5번째 차원 'seasonFit'(0~1)을 산출한다.
///
/// [프로젝트 연동]
///   - 계절 enum 은 프로젝트 전역 Season(CropData.cs)을 그대로 사용한다.
///   - 현재 계절은 TimeManager.Instance.currentSeason 을 실시간으로 읽는다.
///     → 별도 이벤트 연결(OnSeasonChange) 불필요.
///   - 수요 적합도는 CropData.season(작물의 제철)에서 자동 도출한다.
///     → 작물 이름을 손으로 입력하는 표가 필요 없음. CropData 수정도 불필요.
///
/// 규칙: 판매 아이템이 '현재 계절이 제철인 작물'의 수확물이면 수요↑,
///       사철(All)이면 중간, 철 지난 작물이면 수요↓, 매핑 불가 시 기본값.
/// </summary>
public class SeasonalDemand : MonoBehaviour
{
    public static SeasonalDemand Instance;

    [Header("수요 적합도가 적용될 작물 목록 (CropData 에셋 드래그)")]
    [Tooltip("각 CropData 의 수확 아이템(harvestItem / best / normal / trash)을 자동으로 계절에 매핑한다.")]
    [SerializeField] private List<CropData> crops = new List<CropData>();

    [Header("수요 적합도 값 (0~1)")]
    [Range(0f, 1f)] [SerializeField] private float inSeasonFit = 0.90f; // 제철
    [Range(0f, 1f)] [SerializeField] private float allSeasonFit = 0.70f; // 사철(Season.All)
    [Range(0f, 1f)] [SerializeField] private float offSeasonFit = 0.40f; // 철 지남
    [Range(0f, 1f)] [SerializeField] private float defaultFit = 0.60f; // 표에 없는 아이템

    [Header("디버그")]
    [SerializeField] private bool logDebug = false;

    // 수확 아이템 → 그 작물의 제철(Season)
    private Dictionary<ItemData, Season> itemSeason;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        BuildMap();
    }

    private void BuildMap()
    {
        itemSeason = new Dictionary<ItemData, Season>();
        foreach (var crop in crops)
        {
            if (crop == null) continue;
            Register(crop.harvestItem, crop.season);
            Register(crop.bestHarvestItem, crop.season);
            Register(crop.normalHarvestItem, crop.season);
            Register(crop.trashHarvestItem, crop.season);
        }

        if (logDebug)
            Debug.Log($"[SeasonalDemand] 매핑 완료: 작물 {crops.Count}종 → 아이템 {itemSeason.Count}개");
    }

    private void Register(ItemData item, Season season)
    {
        if (item == null) return;
        if (!itemSeason.ContainsKey(item)) // 먼저 등록된 작물 우선
            itemSeason[item] = season;
    }

    /// <summary>현재 계절 기준, 해당 아이템의 수요 적합도(0~1).</summary>
    public float GetDemandFit(ItemData item)
    {
        if (item == null) return defaultFit;
        if (itemSeason == null) BuildMap();
        if (TimeManager.Instance == null) return defaultFit;

        if (!itemSeason.TryGetValue(item, out var cropSeason))
            return defaultFit;

        if (cropSeason == Season.All) return allSeasonFit;
        return cropSeason == TimeManager.Instance.currentSeason ? inSeasonFit : offSeasonFit;
    }

    /// <summary>런타임에 작물 목록을 바꿨을 때 매핑을 다시 만든다(선택).</summary>
    public void Rebuild() => BuildMap();
}