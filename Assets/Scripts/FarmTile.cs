using UnityEngine;
using System.Collections;

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

    [Header("결과 표시 시간 (초)")]
    public float resultDisplayDuration = 1.5f;

    [Header("리듬게임 중 Sorting Order")]
    [Tooltip("수확 애니메이션 중에 작물이 플레이어 위에 보이도록")]
    public int harvestSortingOrder = 100;

    private SpriteRenderer spriteRenderer;
    private int originalSortingOrder = 0;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalSortingOrder = spriteRenderer.sortingOrder;

        if (FarmTileManager.Instance != null)
            FarmTileManager.Instance.RegisterTile(this);
    }

    public bool Till()
    {
        if (state != TileState.Normal) return false;
        state = TileState.Tilled;
        return true;
    }

    public bool Reset()
    {
        if (state == TileState.Normal) return false;
        if (state == TileState.Seeded || state == TileState.SeedWatered)
        {
            cropData = null;
            currentGrowthDay = 0;
        }
        state = TileState.Normal;
        return true;
    }

    public bool Plant(CropData crop)
    {
        if (state != TileState.Tilled && state != TileState.Watered) return false;

        if (crop.season != Season.All && crop.season != TimeManager.Instance.currentSeason)
        {
            Debug.Log($"이 계절에는 {crop.cropName}을 심을 수 없어요!");
            return false;
        }

        cropData = crop;
        state = state == TileState.Watered ? TileState.SeedWatered : TileState.Seeded;
        UpdateSprite();
        return true;
    }

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
        return false;
    }

    public void OnDayPass(Vector3Int cellPos)
    {
        if (state == TileState.SeedWatered)
        {
            currentGrowthDay++;

            if (cropData != null && currentGrowthDay >= cropData.growthDays)
            {
                state = TileState.Grown;
            }
            else
            {
                state = TileState.Seeded;
                PestGrowthEventManager.Instance?.TryRollPestEvent(
                    cellPos, currentGrowthDay, cropData.growthDays);
            }
            UpdateSprite();
        }
        else if (state == TileState.Watered)
        {
            state = TileState.Tilled;
        }
    }

    public bool Harvest()
    {
        if (state != TileState.Grown) return false;

        if (cropData.harvestItem != null)
        {
            int amount = cropData.baseHarvestAmount;
            InventoryManager.Instance.AddItem(cropData.harvestItem, amount);
            Debug.Log($"{cropData.cropName} 수확! x{amount}");
        }

        cropData = null;
        currentGrowthDay = 0;
        state = TileState.Tilled;
        UpdateSprite();
        return true;
    }

    // ─────────────────────────────────────────────
    // ★ 리듬게임 애니메이션용 sprite 변경
    // sortingOrder도 같이 올려서 플레이어 위에 표시
    // ─────────────────────────────────────────────
    public void SetHarvestSprite(Sprite sprite)
    {
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = harvestSortingOrder;  // 플레이어 위로
        }
    }

    public bool HarvestWithQuality(CropQuality quality, Vector3Int cellPos)
    {
        if (state != TileState.Grown) return false;

        HarvestRhythmResult finalResult = HarvestRhythmResult.Normal;

        if (cropData != null)
        {
            HarvestRhythmResult rhythmResult =
                quality == CropQuality.Best ? HarvestRhythmResult.Best :
                quality == CropQuality.Normal ? HarvestRhythmResult.Normal :
                                                HarvestRhythmResult.Trash;

            finalResult = rhythmResult;
            int finalYield = cropData.baseYield > 0 ? cropData.baseYield : 1;

            if (PestGrowthEventManager.Instance != null)
            {
                finalResult = PestGrowthEventManager.Instance
                    .ApplyPestQualityBonus(cellPos, rhythmResult);

                int penalty = PestGrowthEventManager.Instance.GetYieldPenalty(cellPos);
                finalYield = Mathf.Max(1, finalYield - penalty);

                if (penalty > 0)
                    Debug.Log($"수확량 패널티 적용! -{penalty}");
            }

            ItemData rewardItem = cropData.GetHarvestItemByResult(finalResult);

            if (rewardItem != null)
            {
                int leftover = InventoryManager.Instance
                    .AddItemAndReturnLeftover(rewardItem, finalYield);

                if (leftover > 0)
                    ItemDropManager.Instance?.DropItemFromHarvest(
                        rewardItem, leftover, transform.position);

                Debug.Log($"{cropData.cropName} 수확! 등급:{finalResult} x{finalYield}");
            }

            PestGrowthEventManager.Instance?.ClearEventData(cellPos);
        }

        StartCoroutine(ClearAfterDelay());

        return true;
    }

    IEnumerator ClearAfterDelay()
    {
        yield return new WaitForSeconds(resultDisplayDuration);

        cropData = null;
        currentGrowthDay = 0;
        state = TileState.Tilled;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = null;
            spriteRenderer.sortingOrder = originalSortingOrder;  // ★ 원래대로
        }
        UpdateSprite();
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        if (cropData == null || cropData.growthSprites == null) return;
        int stage = Mathf.Clamp(currentGrowthDay, 0, cropData.growthSprites.Length - 1);
        spriteRenderer.sprite = cropData.growthSprites[stage];
    }

    void OnDestroy()
    {
        if (FarmTileManager.Instance != null)
            FarmTileManager.Instance.UnregisterTile(this);
    }
}