using UnityEngine;

public enum ItemType
{
    Tool,   // µµ±¸ (±ªÀÌ, ¹°»Ñ¸®°³ µî)
    Seed,   // ¾¾¾Ñ
    Crop,   // ÀÛ¹°
    None
}

public enum ToolType
{
    None,
    Hoe,          // ±ªÀÌ
    WateringCan,  // ¹°»Ñ¸®°³
    Scythe,       // ³´
    Pickaxe       // °î±ªÀÌ
}

[CreateAssetMenu(fileName = "ItemData", menuName = "Farm/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("¾ÆÀÌÅÛ Á¤º¸")]
    public string itemName;
    public Sprite itemSprite;
    public ItemType itemType;

    [Header("µµ±¸ ¼³Á¤")]
    public ToolType toolType;

    [Header("¾¾¾Ñ ¼³Á¤")]
    public CropData cropData; // ¾¾¾ÑÀÌ¸é ¾î¶² ÀÛ¹°ÀÎÁö
}
