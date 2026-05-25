using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [Header("━━ 타일맵 참조 ━━")]
    public Tilemap ground;
    public Tilemap farmland;
    public Tilemap farmable;
    public Tilemap hoed;
    public Tilemap watered;
    public Tilemap crop;
    public Tilemap objects;
    public Tilemap collision;

    [Header("━━ 기본 타일 ━━")]
    public TileBase groundTile;
    public TileBase farmTile;
    public TileBase waterTile;
    public TileBase buildingTile;

    [Header("━━ 절벽 타일 세트 ━━")]
    [Tooltip("Tileset_87")] public TileBase cliffTopLeft;
    [Tooltip("Tileset_89")] public TileBase cliffTop;
    [Tooltip("Tileset_90")] public TileBase cliffTopRight;
    [Tooltip("Tileset_91")] public TileBase cliffLeft;
    [Tooltip("Tileset_97")] public TileBase cliffRight;
    [Tooltip("Tileset_98")] public TileBase cliffBottomLeft;
    [Tooltip("Tileset_99")] public TileBase cliffBottom;
    [Tooltip("Tileset_101")] public TileBase cliffBottomRight;

    [Header("━━ 맵 크기 ━━")]
    public int mapWidth = 40;
    public int mapHeight = 30;

    // 절벽 두께
    const int CLIFF = 3;

    // 구역 좌표
    static readonly RectInt HOUSE = new RectInt(16, 24, 6, 5);
    static readonly RectInt HOUSE_FRONT = new RectInt(15, 21, 8, 3);
    static readonly RectInt BARN = new RectInt(24, 23, 5, 4);
    static readonly RectInt MAIN_FARM = new RectInt(5, 12, 12, 8);
    static readonly RectInt SUB_FARM = new RectInt(22, 14, 8, 6);
    static readonly RectInt POND_1 = new RectInt(17, 13, 4, 3);
    static readonly RectInt POND_2 = new RectInt(28, 8, 4, 3);

    public static readonly Vector3Int PLAYER_START = new Vector3Int(19, 20, 0);

    [ContextMenu("★ Generate Map")]
    public void GenerateMap()
    {
        if (!Validate()) return;

        ClearAll();

        Step1_Ground();
        Step2_Cliff();      // ← 나무 대신 절벽!
        Step3_House();
        Step4_MainFarm();
        Step5_SubFarm();
        Step6_Ponds();
        Step7_OpenArea();

        PrintSummary();
    }

    // 1. 전체 바닥
    void Step1_Ground()
    {
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                Set(ground, x, y, groundTile);

        Debug.Log($"[1/7] ✅ 바닥 {mapWidth}×{mapHeight}");
    }

    // 2. 절벽 외곽 — 방향별 타일 자동 배치
    void Step2_Cliff()
    {
        int maxX = mapWidth - 1;
        int maxY = mapHeight - 1;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                bool onLeft = x < CLIFF;
                bool onRight = x > maxX - CLIFF;
                bool onBottom = y < CLIFF;
                bool onTop = y > maxY - CLIFF;

                if (!onLeft && !onRight && !onBottom && !onTop) continue;

                TileBase tile = GetCliffTile(onLeft, onRight, onTop, onBottom);
                if (tile == null) continue;

                Set(objects, x, y, tile);
                Set(collision, x, y, tile);
            }
        }

        Debug.Log("[2/7] ✅ 절벽 외곽");
    }

    // 방향에 따라 맞는 절벽 타일 반환
    TileBase GetCliffTile(bool left, bool right, bool top, bool bottom)
    {
        // 모서리
        if (top && left) return cliffTopLeft;
        if (top && right) return cliffTopRight;
        if (bottom && left) return cliffBottomLeft;
        if (bottom && right) return cliffBottomRight;

        // 변
        if (top) return cliffTop;
        if (bottom) return cliffBottom;
        if (left) return cliffLeft;
        if (right) return cliffRight;

        return null;
    }

    // 3. 집 + 창고
    void Step3_House()
    {
        Fill(farmland, HOUSE_FRONT, farmTile);
        Fill(objects, HOUSE, buildingTile);
        Fill(collision, HOUSE, buildingTile);
        Fill(objects, BARN, buildingTile);
        Fill(collision, BARN, buildingTile);

        // 창고 앞마당
        Fill(farmland, new RectInt(BARN.x, BARN.y - 2, BARN.width, 2), farmTile);

        Debug.Log("[3/7] ✅ 집 + 창고");
    }

    // 4. 메인 농장 ★
    void Step4_MainFarm()
    {
        Fill(farmable, MAIN_FARM, farmTile);
        Debug.Log($"[4/7] ✅ 메인 밭 {MAIN_FARM.width}×{MAIN_FARM.height}");
    }

    // 5. 보조 농장
    void Step5_SubFarm()
    {
        Fill(farmable, SUB_FARM, farmTile);
        Debug.Log($"[5/7] ✅ 보조 밭 {SUB_FARM.width}×{SUB_FARM.height}");
    }

    // 6. 연못
    void Step6_Ponds()
    {
        if (waterTile != null)
        {
            Fill(objects, POND_1, waterTile);
            Fill(collision, POND_1, waterTile);
            Fill(objects, POND_2, waterTile);
            Fill(collision, POND_2, waterTile);
        }
        Debug.Log("[6/7] ✅ 연못");
    }

    // 7. 열린 공간 연결
    void Step7_OpenArea()
    {
        Fill(farmland, new RectInt(5, 20, 26, 2), farmTile);
        Debug.Log("[7/7] ✅ 열린 공간");
    }

    // ═══ 유틸 ═══
    void Set(Tilemap map, int x, int y, TileBase tile)
    {
        if (map == null || tile == null) return;
        map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    void Fill(Tilemap map, RectInt r, TileBase tile)
    {
        if (map == null || tile == null) return;
        for (int x = r.x; x < r.x + r.width; x++)
            for (int y = r.y; y < r.y + r.height; y++)
                map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    void ClearAll()
    {
        ground?.ClearAllTiles();
        farmland?.ClearAllTiles();
        farmable?.ClearAllTiles();
        hoed?.ClearAllTiles();
        watered?.ClearAllTiles();
        crop?.ClearAllTiles();
        objects?.ClearAllTiles();
        collision?.ClearAllTiles();
        Debug.Log("[Clear] 초기화 완료");
    }

    void PrintSummary()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🌱 맵 생성 완료!");
        Debug.Log($"   Player → {PLAYER_START} 으로 이동");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }

    bool Validate()
    {
        bool ok = true;
        if (!ground) { Debug.LogError("❌ ground 없음!"); ok = false; }
        if (!farmable) { Debug.LogError("❌ farmable 없음!"); ok = false; }
        if (!objects) { Debug.LogError("❌ objects 없음!"); ok = false; }
        if (!collision) { Debug.LogError("❌ collision 없음!"); ok = false; }
        if (!groundTile) { Debug.LogError("❌ groundTile 없음!"); ok = false; }
        if (!farmTile) { Debug.LogError("❌ farmTile 없음!"); ok = false; }
        if (!buildingTile) { Debug.LogError("❌ buildingTile 없음!"); ok = false; }
        if (!cliffTopLeft) { Debug.LogError("❌ cliffTopLeft(Tileset_87) 없음!"); ok = false; }
        if (!cliffTop) { Debug.LogError("❌ cliffTop(Tileset_89) 없음!"); ok = false; }
        if (!cliffTopRight) { Debug.LogError("❌ cliffTopRight(Tileset_90) 없음!"); ok = false; }
        if (!cliffLeft) { Debug.LogError("❌ cliffLeft(Tileset_91) 없음!"); ok = false; }
        if (!cliffRight) { Debug.LogError("❌ cliffRight(Tileset_97) 없음!"); ok = false; }
        if (!cliffBottomLeft) { Debug.LogError("❌ cliffBottomLeft(Tileset_98) 없음!"); ok = false; }
        if (!cliffBottom) { Debug.LogError("❌ cliffBottom(Tileset_99) 없음!"); ok = false; }
        if (!cliffBottomRight) { Debug.LogError("❌ cliffBottomRight(Tileset_101) 없음!"); ok = false; }
        return ok;
    }
}