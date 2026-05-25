using UnityEngine;
using System.Collections.Generic;

public class FarmTileManager : MonoBehaviour
{
    public static FarmTileManager Instance;

    private List<FarmTile> activeTiles = new List<FarmTile>();

    void Awake()
    {
        Instance = this;
    }

    // FarmTile 등록
    public void RegisterTile(FarmTile tile)
    {
        if (!activeTiles.Contains(tile))
            activeTiles.Add(tile);
    }

    // FarmTile 해제
    public void UnregisterTile(FarmTile tile)
    {
        activeTiles.Remove(tile);
    }

    // 하루 지나면 모든 타일 성장
    public void OnDayPass()
    {
        foreach (FarmTile tile in activeTiles)
        {
            if (tile == null) continue;
            Vector3Int cellPos = TileManager.Instance.WorldToCell(tile.transform.position);
            tile.OnDayPass(cellPos); // ★ cellPos 전달
        }
        RefreshAllTiles();
        Debug.Log($"하루 지남! 활성 타일: {activeTiles.Count}개");
    }

    void RefreshAllTiles()
    {
        foreach (FarmTile tile in activeTiles)
        {
            if (tile == null) continue;
            Vector3Int cellPos = TileManager.Instance.WorldToCell(tile.transform.position);
            TileManager.Instance.RefreshTile(cellPos, tile.state);
        }
    }
}