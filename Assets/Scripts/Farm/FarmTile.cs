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

    [Header("작물 그림")]
    [Tooltip("작물 그림의 Sorting Order")]
    public int cropSortingOrder = 5;
    [Tooltip("밑동을 흙 정중앙보다 살짝 내리려면 음수 (0이면 정중앙)")]
    public float baseYNudge = -0.25f;

    [Tooltip("플레이어와 동일한 정렬 레이어 (Y정렬로 앞뒤 가림)")]
    public string entitySortingLayer = "Entities";

    private SpriteRenderer cropRenderer;   // 작물 그림 전용 (자식 오브젝트)
    private int originalSortingOrder = 0;

    void Awake()
    {
        // 작물 그림을 본체가 아닌 자식 오브젝트에서 그린다.
        // (본체/콜라이더는 타일 중앙에 고정 → 클릭 판정 안정, 그림만 자유롭게 정렬)
        GameObject vis = new GameObject("CropVisual");
        vis.transform.SetParent(transform, false);
        vis.transform.localPosition = Vector3.zero;

        cropRenderer = vis.AddComponent<SpriteRenderer>();
        cropRenderer.sortingLayerName = entitySortingLayer;
        cropRenderer.sortingOrder = cropSortingOrder;
        originalSortingOrder = cropSortingOrder;

        if (FarmTileManager.Instance != null)
            FarmTileManager.Instance.RegisterTile(this);
    }

    // 작물 스프라이트를 자식에 세팅하면서, Pivot/프레임 크기와 무관하게
    // "밑동(맨 아랫줄)"이 항상 흙 중앙에 오도록 위치를 자동 보정한다.
    void SetCropSprite(Sprite sp)
    {
        if (cropRenderer == null) return;
        cropRenderer.sprite = sp;
        if (sp == null) return;

        float ppu = sp.pixelsPerUnit;
        float x = (sp.pivot.x - sp.rect.width / 2f) / ppu;   // 가로 중앙 정렬
        float y = sp.pivot.y / ppu + baseYNudge;             // 밑동을 흙 중앙에
        cropRenderer.transform.localPosition = new Vector3(x, y, 0f);

        ApplyYSorting();
    }

    // 작물 밑동의 월드 Y에 따라 Sorting Order 결정 (스듀식 앞뒤 가림).
    // Y가 낮을수록(화면 아래쪽) Order가 커져서 앞에 그려진다.
    // 플레이어(Order 11) 기준으로: 플레이어가 작물보다 위에 있으면 작물이 앞(가림),
    // 아래로 내려오면 작물이 뒤(플레이어가 가림).
    void ApplyYSorting()
    {
        if (cropRenderer == null) return;
        cropRenderer.sortingLayerName = entitySortingLayer;  // 플레이어와 같은 레이어
        if (cropRenderer.sortingOrder == harvestSortingOrder) return; // 수확 모션 중엔 건드리지 않음

        float baseWorldY = transform.position.y + baseYNudge; // 작물 밑동 높이
        cropRenderer.sortingOrder = Mathf.RoundToInt(-baseWorldY * 100f);
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
            if (cropRenderer != null)
            {
                cropRenderer.sprite = null;
                cropRenderer.color = Color.white;
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
        if (cropRenderer == null) return;

        if (witheredSprite != null)
        {
            SetCropSprite(witheredSprite);
            cropRenderer.color = Color.white;
        }
        else
        {
            // 시든 스프라이트가 없으면 현재 스프라이트를 갈색/회색으로 어둡게
            cropRenderer.color = new Color(0.45f, 0.38f, 0.30f);
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
        if (cropRenderer != null && sprite != null)
        {
            SetCropSprite(sprite);
            cropRenderer.sortingOrder = harvestSortingOrder;  // 플레이어 위로
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

        if (cropRenderer != null)
        {
            cropRenderer.sprite = null;
            cropRenderer.color = Color.white;
            cropRenderer.sortingOrder = originalSortingOrder;  // ★ 원래대로
        }
        UpdateSprite();
    }

    void UpdateSprite()
    {
        if (cropRenderer == null) return;
        if (cropData == null || cropData.growthSprites == null) return;
        int stage = Mathf.Clamp(currentGrowthDay, 0, cropData.growthSprites.Length - 1);
        SetCropSprite(cropData.growthSprites[stage]);
    }

    void OnDestroy()
    {
        if (FarmTileManager.Instance != null)
            FarmTileManager.Instance.UnregisterTile(this);
    }
}