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

    public void RegisterTile(FarmTile tile)
    {
        if (!activeTiles.Contains(tile))
            activeTiles.Add(tile);
    }

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
            tile.OnDayPass(cellPos);
        }
        RefreshAllTiles();
        Debug.Log($"하루 지남! 활성 타일: {activeTiles.Count}개");
    }

    // ─────────────────────────────────────────────
    // ★ 계절이 바뀔 때 호출 (TimeManager.OnSeasonChange에 연결)
    //   심어진 작물을 전부 시들게 함
    // ─────────────────────────────────────────────
    public void OnSeasonChange()
    {
        int withered = 0;
        foreach (FarmTile tile in activeTiles)
        {
            if (tile == null) continue;
            tile.WitherIfPlanted();
            if (tile.state == FarmTile.TileState.Withered) withered++;
        }
        RefreshAllTiles();
        Debug.Log($"계절 변경 — 작물 {withered}개 시듦");
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