using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance;

    [Header("타일맵")]
    public Tilemap groundTilemap;       // 좌표 계산 기준 (계절 맵 중 아무거나 1개 연결해두면 됨)
    public Tilemap farmableTilemap;     // 농사 가능한 흙 타일
    public Tilemap farmlandTilemap;     // 경작 후 상태 표시

    [Header("타일")]
    public TileBase tilledTile;
    public TileBase wateredTile;

    void Awake()
    {
        Instance = this;
    }

    // ───────── 좌표 변환 ─────────
    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return groundTilemap.WorldToCell(worldPos);
    }

    public Vector3 CellCenter(Vector3Int cellPos)
    {
        return groundTilemap.GetCellCenterWorld(cellPos);
    }

    // ───────── 타일 판별 ─────────
    public bool IsFarmable(Vector3Int cellPos)
    {
        return farmableTilemap.HasTile(cellPos);
    }

    public bool IsGround(Vector3Int cellPos)
    {
        return IsFarmable(cellPos);
    }

    public bool IsTilled(Vector3Int cellPos)
    {
        return farmlandTilemap.HasTile(cellPos);
    }

    // ───────── 타일 상태 변경 ─────────
    static readonly Color COLOR_TILLED = new Color(0.7f, 0.5f, 0.3f);
    static readonly Color COLOR_WATERED = new Color(0.4f, 0.5f, 0.7f);

    public void SetTilled(Vector3Int cellPos)
    {
        TileBase baseTile = farmableTilemap.GetTile(cellPos);
        farmlandTilemap.SetTile(cellPos, baseTile);
        farmlandTilemap.SetTileFlags(cellPos, TileFlags.None);

        Color originalColor = farmableTilemap.GetColor(cellPos);
        farmlandTilemap.SetColor(cellPos, originalColor * COLOR_TILLED);
    }

    public void SetWatered(Vector3Int cellPos)
    {
        TileBase baseTile = farmableTilemap.GetTile(cellPos);
        farmlandTilemap.SetTile(cellPos, baseTile);
        farmlandTilemap.SetTileFlags(cellPos, TileFlags.None);

        Color originalColor = farmableTilemap.GetColor(cellPos);
        farmlandTilemap.SetColor(cellPos, originalColor * COLOR_WATERED);
    }

    public void SetNormal(Vector3Int cellPos)
    {
        farmlandTilemap.SetTile(cellPos, null);
    }

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
            case FarmTile.TileState.Withered:   // ★ 시든 작물 아래 흙은 갈아둔 상태 유지
                SetTilled(cellPos);
                break;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (farmableTilemap == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
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