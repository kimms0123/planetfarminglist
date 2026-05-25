using UnityEngine;
using System.Collections.Generic;

public class PestGrowthEventManager : MonoBehaviour
{
    public static PestGrowthEventManager Instance;

    [Header("설정")]
    [Range(0f, 1f)]
    public float pestEventChance = 0.25f;

    [Header("프리팹")]
    public GameObject pestEventIconPrefab;
    public float iconYOffset = 0.8f;

    // 셀 위치별 이벤트 데이터
    private Dictionary<Vector3Int, PestEventData> eventDataMap
        = new Dictionary<Vector3Int, PestEventData>();

    // 셀 위치별 아이콘 오브젝트
    private Dictionary<Vector3Int, GameObject> iconMap
        = new Dictionary<Vector3Int, GameObject>();

    void Awake()
    {
        Instance = this;
    }

    // ─────────────────────────────────────────
    // 해충 이벤트 발생 시도
    // ─────────────────────────────────────────
    public void TryRollPestEvent(Vector3Int cellPos, int currentGrowthDay, int totalGrowthDays)
    {
        // 이미 수확 가능 단계면 발생 금지
        if (currentGrowthDay >= totalGrowthDays) return;

        // 이미 이벤트 있으면 중복 발생 금지
        if (eventDataMap.ContainsKey(cellPos)) return;

        // 확률 검사
        if (Random.value > pestEventChance) return;

        // 이벤트 생성
        var data = new PestEventData();
        eventDataMap[cellPos] = data;

        // 아이콘 생성
        SpawnIcon(cellPos);

        Debug.Log($"해충 이벤트 발생! 셀:{cellPos}");
    }

    // ─────────────────────────────────────────
    // 아이콘 생성
    // ─────────────────────────────────────────
    void SpawnIcon(Vector3Int cellPos)
    {
        if (pestEventIconPrefab == null)
        {
            Debug.LogWarning("pestEventIconPrefab이 없어요!");
            return;
        }

        Vector3 worldPos = TileManager.Instance.CellCenter(cellPos);
        worldPos.y += iconYOffset;

        GameObject icon = Instantiate(pestEventIconPrefab, worldPos, Quaternion.identity);
        PestEventIcon pestIcon = icon.GetComponent<PestEventIcon>();
        pestIcon?.Init(cellPos);

        iconMap[cellPos] = icon;

        Debug.Log($"해충 이벤트 아이콘 생성! 위치:{worldPos}");
    }

    // ─────────────────────────────────────────
    // 아이콘 클릭 → 미니게임 시작
    // ─────────────────────────────────────────
    public void OnIconClicked(Vector3Int cellPos)
    {
        if (!eventDataMap.ContainsKey(cellPos)) return;
        if (PestTimingMiniGame.Instance != null && PestTimingMiniGame.Instance.IsPlaying) return;

        Debug.Log("해충 박멸 미니게임 시작!");
        PestTimingMiniGame.Instance?.StartMiniGame(cellPos);
    }

    // ─────────────────────────────────────────
    // 미니게임 결과 저장
    // ─────────────────────────────────────────
    public void OnMiniGameResult(Vector3Int cellPos, PestEventResult result)
    {
        if (!eventDataMap.ContainsKey(cellPos)) return;

        var data = eventDataMap[cellPos];
        data.result = result;

        switch (result)
        {
            case PestEventResult.Success:
                data.qualityBonus = 1;
                Debug.Log("해충 이벤트 성공: 품질 보너스 +1");
                break;
            case PestEventResult.Neutral:
                Debug.Log("해충 이벤트 보통: 변화 없음");
                break;
            case PestEventResult.Fail:
                data.yieldPenalty = 1;
                Debug.Log("해충 이벤트 실패: 수확량 패널티 +1");
                break;
        }

        // 아이콘 제거
        RemoveIcon(cellPos);
    }

    // ─────────────────────────────────────────
    // 수확 시 품질 보정
    // ─────────────────────────────────────────
    public HarvestRhythmResult ApplyPestQualityBonus(
        Vector3Int cellPos, HarvestRhythmResult result)
    {
        if (!eventDataMap.ContainsKey(cellPos)) return result;

        int bonus = eventDataMap[cellPos].qualityBonus;
        if (bonus <= 0) return result;

        Debug.Log("수확 등급 보정 적용!");

        switch (result)
        {
            case HarvestRhythmResult.Trash: return HarvestRhythmResult.Normal;
            case HarvestRhythmResult.Normal: return HarvestRhythmResult.Best;
            default: return HarvestRhythmResult.Best;
        }
    }

    // ─────────────────────────────────────────
    // 수확 시 수확량 패널티
    // ─────────────────────────────────────────
    public int GetYieldPenalty(Vector3Int cellPos)
    {
        if (!eventDataMap.ContainsKey(cellPos)) return 0;
        return eventDataMap[cellPos].yieldPenalty;
    }

    // ─────────────────────────────────────────
    // 수확 후 데이터 삭제
    // ─────────────────────────────────────────
    public void ClearEventData(Vector3Int cellPos)
    {
        eventDataMap.Remove(cellPos);
        RemoveIcon(cellPos);
        Debug.Log($"이벤트 데이터 삭제! 셀:{cellPos}");
    }

    void RemoveIcon(Vector3Int cellPos)
    {
        if (iconMap.TryGetValue(cellPos, out GameObject icon))
        {
            if (icon != null) Destroy(icon);
            iconMap.Remove(cellPos);
        }
    }
}

// 이벤트 데이터 클래스
public class PestEventData
{
    public PestEventResult result = PestEventResult.None;
    public int qualityBonus = 0;
    public int yieldPenalty = 0;
}