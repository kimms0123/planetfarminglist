using UnityEngine;
using System.Collections.Generic;


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

    public void Rebuild() => BuildMap();
}