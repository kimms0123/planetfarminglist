using UnityEngine;

public enum Season
{
    Spring, Summer, Fall, Winter, All
}

public enum CropQuality
{
    Trash, Normal, Good, Great, Best
}

[CreateAssetMenu(fileName = "CropData", menuName = "Farm/CropData")]
public class CropData : ScriptableObject
{
    [Header("기본 정보")]
    public string cropName;
    public Season season;
    public int growthDays;
    public Sprite[] growthSprites;

    [Header("수확 아이템")]
    public ItemData harvestItem;
    public int baseHarvestAmount = 1;

    [Header("판매 정보")]
    public float cropCoefficient = 1f;
    public int baseSellPrice;

    // ─────────────────────────────────────────────
    // 리듬게임 수확 시퀀스 sprite
    // ─────────────────────────────────────────────
    [Header("리듬게임 수확 시퀀스")]
    [Tooltip("수확1, 수확2, 수확3 - 노트 입력 시 재생되는 시퀀스")]
    public Sprite[] harvestStageSprites;

    [Tooltip("수확4 - 다시 박힘 (일반 노트 사이클 끝)")]
    public Sprite harvestFailSprite;

    [Header("리듬게임 결과 시퀀스 (마지막 노트 성공 시)")]
    [Tooltip("결과1, 결과2, 결과3 - 뽁! 하고 뽑히는 시퀀스")]
    public Sprite[] harvestResultSprites;

    [Tooltip("일반 등급 - 결과 시퀀스에서 멈출 인덱스 (보통 1 = 결과2)")]
    public int normalResultEndIndex = 1;

    [Tooltip("최상급 등급 - 결과 시퀀스에서 멈출 인덱스 (보통 2 = 결과3)")]
    public int bestResultEndIndex = 2;

    [Header("품질 배수")]
    public static readonly float[] qualityMultiplier =
    {
        0.3f, 1.0f, 1.3f, 1.5f, 2.0f
    };

    [Header("수확량 배수")]
    public static readonly float[] harvestMultiplier =
    {
        0.5f, 1.0f, 1.1f, 1.2f, 1.5f
    };

    public static CropQuality GetQuality(float perfectRatio)
    {
        if (perfectRatio >= 0.9f) return CropQuality.Best;
        if (perfectRatio >= 0.7f) return CropQuality.Great;
        if (perfectRatio >= 0.4f) return CropQuality.Normal;
        return CropQuality.Trash;
    }

    public int GetSellPrice(CropQuality quality)
    {
        return Mathf.RoundToInt(baseSellPrice * qualityMultiplier[(int)quality]);
    }

    public int GetHarvestAmount(CropQuality quality)
    {
        return Mathf.RoundToInt(baseHarvestAmount * harvestMultiplier[(int)quality]);
    }

    [Header("등급별 수확 아이템")]
    public ItemData bestHarvestItem;
    public ItemData normalHarvestItem;
    public ItemData trashHarvestItem;
    public int baseYield = 1;

    public ItemData GetHarvestItemByResult(HarvestRhythmResult result)
    {
        switch (result)
        {
            case HarvestRhythmResult.Best:
                return bestHarvestItem ?? normalHarvestItem;
            case HarvestRhythmResult.Trash:
                return trashHarvestItem ?? normalHarvestItem;
            default:
                return normalHarvestItem;
        }
    }

    // ─────────────────────────────────────────────
    // 결과에 따른 결과 시퀀스 마지막 인덱스 반환
    // ─────────────────────────────────────────────
    public int GetResultEndIndex(HarvestRhythmResult result)
    {
        switch (result)
        {
            case HarvestRhythmResult.Best:
                return Mathf.Clamp(bestResultEndIndex, 0,
                    (harvestResultSprites?.Length ?? 1) - 1);
            case HarvestRhythmResult.Normal:
                return Mathf.Clamp(normalResultEndIndex, 0,
                    (harvestResultSprites?.Length ?? 1) - 1);
            default:
                return 0;
        }
    }
}