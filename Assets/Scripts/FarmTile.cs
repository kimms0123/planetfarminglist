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
        // FarmTileManager에 등록
        if (FarmTileManager.Instance != null)
            FarmTileManager.Instance.RegisterTile(this);
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

        // 계절 체크
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

                // ★ 해충 이벤트 발생 시도
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

    // 수확
    public bool Harvest()
    {
        if (state != TileState.Grown) return false;

        // 인벤토리에 수확물 추가
        if (cropData.harvestItem != null)
        {
            int amount = cropData.baseHarvestAmount;
            InventoryManager.Instance.AddItem(cropData.harvestItem, amount);
            Debug.Log($"{cropData.cropName} 수확! x{amount}");
        }
        else
        {
            Debug.Log($"{cropData.cropName} 수확! (수확물 아이템 미설정)");
        }

        cropData = null;
        currentGrowthDay = 0;
        state = TileState.Tilled;
        UpdateSprite();
        return true;
    }
    public bool HarvestWithQuality(CropQuality quality, Vector3Int cellPos)
    {
        if (state != TileState.Grown) return false;

        if (cropData != null)
        {
            // 1. 리듬게임 결과를 HarvestRhythmResult로 변환
            HarvestRhythmResult rhythmResult =
                quality == CropQuality.Best ? HarvestRhythmResult.Best :
                quality == CropQuality.Normal ? HarvestRhythmResult.Normal :
                                                HarvestRhythmResult.Trash;

            // 2. 해충 이벤트 결과 반영
            HarvestRhythmResult finalResult = rhythmResult;
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

            // 3. 등급별 아이템 선택
            ItemData rewardItem = cropData.GetHarvestItemByResult(finalResult);

            // 4. 인벤토리에 추가
            if (rewardItem != null)
            {
                int leftover = InventoryManager.Instance
                    .AddItemAndReturnLeftover(rewardItem, finalYield);

                if (leftover > 0)
                    ItemDropManager.Instance?.DropItemFromHarvest(
                        rewardItem, leftover, transform.position);

                Debug.Log($"{cropData.cropName} 수확! 등급:{finalResult} x{finalYield}");
            }

            // 5. 해충 데이터 삭제
            PestGrowthEventManager.Instance?.ClearEventData(cellPos);
        }

        cropData = null;
        currentGrowthDay = 0;
        state = TileState.Tilled;
        if (spriteRenderer != null) spriteRenderer.sprite = null;
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
    void OnDestroy()
    {
        if (FarmTileManager.Instance != null)
            FarmTileManager.Instance.UnregisterTile(this);
    }
}