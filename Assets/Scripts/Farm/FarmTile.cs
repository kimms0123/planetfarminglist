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
    [Tooltip("작물 그림의 기본 Sorting Order (Y정렬 적용 전 초기값)")]
    public int cropSortingOrder = 5;
    [Tooltip("밑동을 흙 정중앙보다 살짝 내리려면 음수 (0이면 정중앙)")]
    public float baseYNudge = 0f;
    [Tooltip("플레이어와 동일한 정렬 레이어 (Y정렬로 앞뒤 가림)")]
    public string entitySortingLayer = "Entities";

    [Header("플레이어가 지나갈 때 흔들림 (다 자란 작물만)")]
    [Tooltip("최대 기울기 각도(도)")]
    public float shakeAngle = 10f;
    [Tooltip("흔들림 지속 시간(초)")]
    public float shakeDuration = 0.5f;
    [Tooltip("지속 시간 동안 좌우로 흔들리는 횟수")]
    public float shakeOscillations = 2.5f;
    [Tooltip("이 높이(월드 단위) 미만의 작물은 흔들리지 않음 (키 작은 작물 제외)")]
    public float minShakeHeight = 0.6f;
    [Tooltip("흔들림을 발동시키는 플레이어 태그")]
    public string playerTag = "Player";

    private SpriteRenderer cropRenderer;   // 작물 그림 전용 (자식 오브젝트)
    private Transform cropPivot;           // 밑동 회전축 (흔들림용)
    private int originalSortingOrder = 0;
    private bool isShaking = false;

    void Awake()
    {
        // 작물 그림을 본체가 아닌 자식 오브젝트에서 그린다.
        // (본체/콜라이더는 타일 중앙에 고정 → 클릭 판정 안정, 그림만 자유롭게 정렬)
        // 구조: FarmTile → CropPivot(밑동, 여기서 회전) → CropVisual(그림)
        GameObject pivot = new GameObject("CropPivot");
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = Vector3.zero;
        cropPivot = pivot.transform;

        GameObject vis = new GameObject("CropVisual");
        vis.transform.SetParent(cropPivot, false);
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

        // CropPivot(회전축)을 밑동 높이에 두고, CropVisual은 그 위로 올려
        // 스프라이트 밑동이 정확히 CropPivot 원점에 오게 한다 → 밑동 기준 회전 가능.
        if (cropPivot != null)
            cropPivot.localPosition = new Vector3(0f, baseYNudge, 0f);
        cropRenderer.transform.localPosition = new Vector3(x, sp.pivot.y / ppu, 0f);

        ApplyYSorting();
    }

    // 작물 밑동의 월드 Y에 따라 Sorting Order 결정 (스듀식 앞뒤 가림).
    // Y가 낮을수록(화면 아래쪽) Order가 커져서 앞에 그려진다.
    void ApplyYSorting()
    {
        if (cropRenderer == null) return;
        cropRenderer.sortingLayerName = entitySortingLayer;  // 플레이어와 같은 레이어
        if (cropRenderer.sortingOrder == harvestSortingOrder) return; // 수확 모션 중엔 건드리지 않음

        float baseWorldY = transform.position.y + baseYNudge; // 작물 밑동 높이
        cropRenderer.sortingOrder = Mathf.RoundToInt(-baseWorldY * 100f);
    }

    // ─────────────────────────────────────────────
    // 플레이어가 지나갈 때 흔들림 (다 자란 작물만)
    // ─────────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (state != TileState.Grown) return;   // 다 자란 작물만
        if (isShaking) return;

        // 키 작은 작물은 흔들지 않음 (스프라이트 실제 높이로 자동 판단)
        if (cropRenderer != null && cropRenderer.sprite != null)
        {
            float cropHeight = cropRenderer.sprite.rect.height / cropRenderer.sprite.pixelsPerUnit;
            if (cropHeight < minShakeHeight) return;
        }

        // 플레이어가 어느 쪽에서 왔는지에 따라 첫 기울기 방향 결정
        float dir = (other.transform.position.x < transform.position.x) ? 1f : -1f;
        StartCoroutine(ShakeCoroutine(dir));
    }

    IEnumerator ShakeCoroutine(float dir)
    {
        if (cropPivot == null) yield break;
        isShaking = true;

        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float progress = t / shakeDuration;               // 0 → 1
            float damp = 1f - progress;                        // 점점 작아짐 (통통 복귀)
            float angle = dir * shakeAngle
                          * Mathf.Sin(progress * Mathf.PI * 2f * shakeOscillations)
                          * damp;
            cropPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        cropPivot.localRotation = Quaternion.identity;   // 원위치
        isShaking = false;
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
        if (cropPivot != null)
            cropPivot.localRotation = Quaternion.identity;
        UpdateSprite();
    }

    // [디버그/치트용] 즉시 다 자란 상태로 만든다.
    public void ForceGrowToFull()
    {
        if (cropData == null) return;
        int lastStage = (cropData.growthSprites != null && cropData.growthSprites.Length > 0)
            ? cropData.growthSprites.Length - 1
            : cropData.growthDays;
        currentGrowthDay = lastStage;
        state = TileState.Grown;
        UpdateSprite();
    }

    // [디버그/치트용] 한 단계씩 성장시킨다 (F3 누를 때마다 다음 단계).
    public void GrowOneStage()
    {
        if (cropData == null || cropData.growthSprites == null) return;

        int lastStage = cropData.growthSprites.Length - 1;
        if (currentGrowthDay < lastStage)
            currentGrowthDay++;

        // 마지막 단계에 도달하면 다 자란 상태로
        state = (currentGrowthDay >= lastStage) ? TileState.Grown : TileState.Seeded;
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