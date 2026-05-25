using UnityEngine;


public enum ItemType
{
    Tool,    // 도구
    Seed,    // 씨앗
    Crop,    // 작물 (수확물)
    Mineral, // 광물
    None
}

public enum ToolType
{
    None,
    Hoe,          // 괭이 (땅 파기)
    WateringCan,  // 물뿌리개
    Harvester,    // 작물 수확기 (흡입기)
    Pickaxe       // 곡괭이 (경작지 초기화)
}

[CreateAssetMenu(fileName = "ItemData", menuName = "Farm/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;
    public Sprite itemSprite;
    public ItemType itemType;

    [Header("스택 설정")]
    public bool canStack = true;  // 중첩 가능 여부
    public int maxStack = 99;     // 최대 중첩 수량

    [Header("도구 설정")]
    public ToolType toolType;
    public int harvestGridSize = 1; // 수확기: 초반 1 → 업그레이드로 확장

    [Header("씨앗 설정")]
    public CropData cropData;

    [Header("작물/광물 설정")]
    public int sellPrice;
    public CropQuality quality;

    [Header("스태미너")]
    public int staminaCost = 10; // 도구 사용 시 스태미너 소모
    
}