using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance;

    [Header("타일맵")]
    public Tilemap groundTilemap;
    public Tilemap farmlandTilemap;

    [Header("타일")]
    public TileBase tilledTile;    // 경작지 타일
    public TileBase wateredTile;   // 물 준 타일

    void Awake()
    {
        Instance = this;
    }

    // 월드 좌표 → 셀 좌표
    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return groundTilemap.WorldToCell(worldPos);
    }

    // 셀 중앙 월드 좌표
    public Vector3 CellCenter(Vector3Int cellPos)
    {
        return farmlandTilemap.GetCellCenterWorld(cellPos);
    }

    // 일반 땅인지
    public bool IsGround(Vector3Int cellPos)
    {
        return groundTilemap.HasTile(cellPos);
    }

    // 경작지인지
    public bool IsTilled(Vector3Int cellPos)
    {
        return farmlandTilemap.HasTile(cellPos);
    }

    // 경작지로 변경
    public void SetTilled(Vector3Int cellPos)
    {
        farmlandTilemap.SetTile(cellPos, tilledTile);
    }

    // 물 준 경작지로 변경
    public void SetWatered(Vector3Int cellPos)
    {
        farmlandTilemap.SetTile(cellPos, wateredTile);
    }

    // 경작지 초기화
    public void SetNormal(Vector3Int cellPos)
    {
        farmlandTilemap.SetTile(cellPos, null);
    }

    // 타일 상태에 따라 타일맵 업데이트
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
}