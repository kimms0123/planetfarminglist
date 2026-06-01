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
        Grown,
        Withered    // ★ 추가: 계절이 바뀌어 시든 작물 (치울 때까지 남음)
    }

    public TileState state = TileState.Normal;
    public CropData cropData;
    public int currentGrowthDay = 0;

    [Header("결과 표시 시간 (초)")]
    public float resultDisplayDuration = 1.5f;

    [Header("리듬게임 중 Sorting Order")]
    [Tooltip("수확 애니메이션 중에 작물이 플레이어 위에 보이도록")]
    public int harvestSortingOrder = 100;

    [Header("시듦 표시")]
    [Tooltip("시들었을 때 보여줄 스프라이트. 비우면 현재 스프라이트를 회색으로 어둡게 처리")]
    public Sprite witheredSprite;

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

        // 씨/물준씨/시든작물을 치우면 작물 정보 제거
        if (state == TileState.Seeded || state == TileState.SeedWatered || state == TileState.Withered)
        {
            cropData = null;
            currentGrowthDay = 0;

            // 시든 스프라이트/색 원상복구
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = null;
                spriteRenderer.color = Color.white;
            }
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

    // ─────────────────────────────────────────────
    // ★ 계절이 바뀔 때 호출: 심어진 작물이 있으면 시들게 함
    //   (시든 작물은 자리에 그대로 남고, Reset()으로 치워야 사라짐)
    // ─────────────────────────────────────────────
    public void WitherIfPlanted()
    {
        // 작물이 심긴 상태(씨/물준씨/다자람)만 시듦. 빈 흙/물 준 흙은 그대로.
        if (state == TileState.Seeded || state == TileState.SeedWatered || state == TileState.Grown)
        {
            state = TileState.Withered;
            ShowWithered();
        }
    }

    void ShowWithered()
    {
        if (spriteRenderer == null) return;

        if (witheredSprite != null)
        {
            spriteRenderer.sprite = witheredSprite;
            spriteRenderer.color = Color.white;
        }
        else
        {
            // 시든 스프라이트가 없으면 현재 스프라이트를 갈색/회색으로 어둡게
            spriteRenderer.color = new Color(0.45f, 0.38f, 0.30f);
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