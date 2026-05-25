using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance;

    [Header("타일맵")]
    public Tilemap groundTilemap;       // 잔디 + 흙 전체 바닥 (기존 유지)
    public Tilemap farmableTilemap;     // ★ NEW: 농사 가능한 흙 타일만 배치
    public Tilemap farmlandTilemap;     // 경작 후 상태 표시 (기존 유지)

    [Header("타일")]
    public TileBase tilledTile;         // 갈아엎은 경작지 타일
    public TileBase wateredTile;        // 물 준 타일

    void Awake()
    {
        Instance = this;
    }

    // ─────────────────────────────────────────────
    // 좌표 변환
    // ─────────────────────────────────────────────

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return groundTilemap.WorldToCell(worldPos);
    }

    public Vector3 CellCenter(Vector3Int cellPos)
    {
        return groundTilemap.GetCellCenterWorld(cellPos);
    }

    // ─────────────────────────────────────────────
    // 타일 판별
    // ─────────────────────────────────────────────

    /// <summary>
    /// ★ 농사 가능한 흙 타일인지 확인 (주황/갈색 땅만 true)
    /// FarmingSystem의 괭이질 가능 여부 판단에 사용
    /// </summary>
    public bool IsFarmable(Vector3Int cellPos)
    {
        return farmableTilemap.HasTile(cellPos);
    }

    /// <summary>
    /// 기존 IsGround() — 하위 호환용으로 유지
    /// 내부적으로 IsFarmable()로 위임
    /// </summary>
    public bool IsGround(Vector3Int cellPos)
    {
        return IsFarmable(cellPos);
    }

    /// <summary>
    /// 경작지(괭이질 완료) 상태인지 확인
    /// </summary>
    public bool IsTilled(Vector3Int cellPos)
    {
        return farmlandTilemap.HasTile(cellPos);
    }

    // ─────────────────────────────────────────────
    // 타일 상태 변경
    // ─────────────────────────────────────────────

    // 색상 상수 — 원래 색에 곱해서 어둡게
    static readonly Color COLOR_TILLED = new Color(0.7f, 0.5f, 0.3f); // 갈색 어둡게
    static readonly Color COLOR_WATERED = new Color(0.4f, 0.5f, 0.7f); // 파랗게 어둡게

    public void SetTilled(Vector3Int cellPos)
    {
        TileBase baseTile = farmableTilemap.GetTile(cellPos);
        farmlandTilemap.SetTile(cellPos, baseTile);
        farmlandTilemap.SetTileFlags(cellPos, TileFlags.None);

        // 원래 타일 색상 가져오기
        Color originalColor = farmableTilemap.GetColor(cellPos);
        // 원래 색에 곱해서 어둡게
        farmlandTilemap.SetColor(cellPos, originalColor * COLOR_TILLED);
    }

    public void SetWatered(Vector3Int cellPos)
    {
        TileBase baseTile = farmableTilemap.GetTile(cellPos);
        farmlandTilemap.SetTile(cellPos, baseTile);
        farmlandTilemap.SetTileFlags(cellPos, TileFlags.None);

        // 원래 타일 색상 가져오기
        Color originalColor = farmableTilemap.GetColor(cellPos);
        // 원래 색에 곱해서 파랗게 어둡게
        farmlandTilemap.SetColor(cellPos, originalColor * COLOR_WATERED);
    }

    // SetNormal() 수정
    public void SetNormal(Vector3Int cellPos)
    {
        farmlandTilemap.SetTile(cellPos, null);
        // 색상 초기화는 타일 제거시 자동
    }

    // farmableTilemap에서 해당 위치 타일 가져오기 (헬퍼)
    TileBase GetFarmableTile(Vector3Int cellPos)
    {
        return farmableTilemap.GetTile(cellPos);
    }

    /// <summary>
    /// FarmTile 상태에 따라 farmlandTilemap 타일 갱신
    /// </summary>
    public void RefreshTile(Vector3Int cellPos, FarmTile.TileState state)
    {
        switch (state)
        {
            case FarmTile.TileState.Normal:
                SetNormal(cellPos);
                break;
            case FarmTile.TileState.Tilled:
            case FarmTile.TileState.Seeded:
                SetTilled(cellPos);
                break;
            case FarmTile.TileState.Watered:
            case FarmTile.TileState.SeedWatered:
                SetWatered(cellPos);
                break;
        }
    }

    // ─────────────────────────────────────────────
    // 디버그용
    // ─────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (farmableTilemap == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // 주황 반투명
        foreach (var pos in farmableTilemap.cellBounds.allPositionsWithin)
        {
            if (farmableTilemap.HasTile(pos))
            {
                Vector3 world = farmableTilemap.GetCellCenterWorld(pos);
                Gizmos.DrawCube(world, Vector3.one * 0.9f);
            }
        }
    }
#endif
}