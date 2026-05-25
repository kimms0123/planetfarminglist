using UnityEngine;

public enum Season
{
    Spring, // 봄
    Summer, // 여름
    Fall,   // 가을
    Winter, // 겨울
    All     // 전 계절
}

public enum CropQuality
{
    Trash,   // 쓰레기 (0.3)
    Normal,  // 일반 (1.0)
    Good,    // 좋음 (1.3)
    Great,   // 훌륭 (1.5)
    Best     // 최상급 (2.0)
}

[CreateAssetMenu(fileName = "CropData", menuName = "Farm/CropData")]
public class CropData : ScriptableObject
{
    [Header("기본 정보")]
    public string cropName;
    public Season season;
    public int growthDays;
    public Sprite[] growthSprites; // 성장 단계별 스프라이트 (씨앗→새싹→성장→수확 순)

    [Header("수확 아이템")]
    public ItemData harvestItem;     // 수확 시 인벤토리에 추가될 ItemData
    public int baseHarvestAmount = 1; // 기본 수확 개수

    [Header("판매 정보")]
    public float cropCoefficient = 1f; // 작물 계수
    public int baseSellPrice;          // 기본 판매가

    [Header("품질 배수")]
    public static readonly float[] qualityMultiplier =
    {
        0.3f,  // 쓰레기
        1.0f,  // 일반
        1.3f,  // 좋음
        1.5f,  // 훌륭
        2.0f   // 최상급
    };

    [Header("수확량 배수")]
    public static readonly float[] harvestMultiplier =
    {
        0.5f,  // 쓰레기
        1.0f,  // 일반
        1.1f,  // 좋음
        1.2f,  // 훌륭
        1.5f   // 최상급
    };

    // Perfect 비율로 품질 등급 결정
    public static CropQuality GetQuality(float perfectRatio)
    {
        if (perfectRatio >= 0.9f) return CropQuality.Best;
        if (perfectRatio >= 0.7f) return CropQuality.Great;
        if (perfectRatio >= 0.4f) return CropQuality.Normal;
        return CropQuality.Trash;
    }

    // 품질별 판매가 계산
    public int GetSellPrice(CropQuality quality)
    {
        return Mathf.RoundToInt(baseSellPrice * qualityMultiplier[(int)quality]);
    }

    // 품질별 수확량 계산
    public int GetHarvestAmount(CropQuality quality)
    {
        return Mathf.RoundToInt(baseHarvestAmount * harvestMultiplier[(int)quality]);
    }
    [Header("등급별 수확 아이템")]
    public ItemData bestHarvestItem;
    public ItemData normalHarvestItem;
    public ItemData trashHarvestItem;
    public int baseYield = 1;

    // 리듬게임 결과에 맞는 아이템 반환
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
}
