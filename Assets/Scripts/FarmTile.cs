using UnityEngine;

public class FarmTile : MonoBehaviour
{
    public enum TileState
    {
        Normal,
        Tilled,
        Watered,
        Seeded,
        SeedWatered,
        Grown
    }

    public TileState state = TileState.Normal;
    public CropData cropData;
    public int currentGrowthDay = 0;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // 괭이 → 경작지로
    public bool Till()
    {
        if (state != TileState.Normal) return false;
        state = TileState.Tilled;
        return true;
    }

    // 곡괭이 → 경작지 초기화
    public bool Reset()
    {
        if (state == TileState.Normal) return false;
        if (state == TileState.Seeded || state == TileState.SeedWatered)
        {
            // 작물 파괴
            cropData = null;
            currentGrowthDay = 0;
        }
        state = TileState.Normal;
        return true;
    }

    // 씨앗 → 심기
    public bool Plant(CropData crop)
    {
        if (state != TileState.Tilled && state != TileState.Watered) return false;
        cropData = crop;
        state = state == TileState.Watered ? TileState.SeedWatered : TileState.Seeded;
        UpdateSprite();
        return true;
    }

    // 물뿌리개 → 물 주기
    public bool Water()
    {
        if (state == TileState.Tilled)
        {
            state = TileState.Watered;
            return true;
        }
        if (state == TileState.Seeded)
        {
            state = TileState.SeedWatered;
            return true;
        }
        return false; // 이미 물 줬거나 경작지 아님
    }

    // 하루 지나기
    public void OnDayPass()
    {
        if (state == TileState.SeedWatered)
        {
            currentGrowthDay++;
            if (cropData != null && currentGrowthDay >= cropData.growthDays)
                state = TileState.Grown;
            else
                state = TileState.Seeded; // 물 마름
            UpdateSprite();
        }
        else if (state == TileState.Watered)
        {
            state = TileState.Tilled; // 물 마름
        }
    }

    // 수확
    public bool Harvest()
    {
        if (state != TileState.Grown) return false;
        Debug.Log($"{cropData.harvestItemName} 수확! x{cropData.harvestAmount}");
        cropData = null;
        currentGrowthDay = 0;
        state = TileState.Tilled; // 수확 후 경작지로
        UpdateSprite();
        return true;
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        if (cropData == null || cropData.growthSprites == null) return;
        int stage = Mathf.Clamp(currentGrowthDay, 0, cropData.growthSprites.Length - 1);
        spriteRenderer.sprite = cropData.growthSprites[stage];
    }
}