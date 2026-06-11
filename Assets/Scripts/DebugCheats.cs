#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

// ─────────────────────────────────────────────
// 개발용 치트 (에디터 전용 — #if UNITY_EDITOR로 감싸 출시 빌드엔 안 들어감)
//   F1 : 농사 가능 지역(Farmable) 전부 갈기
//   F2 : 손에 든 씨앗을 인벤 개수만큼 갈린 밭에 심기
//   F3 : 모든 작물을 한 단계씩 성장 (누를 때마다 다음 단계)
//
// 빈 오브젝트에 이 스크립트 하나만 붙이면 됨.
// ─────────────────────────────────────────────
public class DebugCheats : MonoBehaviour
{
    [Header("치트 적용 범위")]
    [Tooltip("플레이어 주변 몇 칸(반경)까지 갈지. 0 이하면 맵 전체")]
    public int tillRadius = 0;   // 0 = farmable 전체
    [Tooltip("범위 기준이 될 플레이어 태그")]
    public string playerTag = "Player";

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f1Key.wasPressedThisFrame) TillAllFarmable();
        if (Keyboard.current.f2Key.wasPressedThisFrame) PlantHeldSeedEverywhere();
        if (Keyboard.current.f3Key.wasPressedThisFrame) GrowAllCrops();
    }

    // F1 : 농사 가능한 칸을 전부 찾아 FarmTile 생성 + 갈기
    void TillAllFarmable()
    {
        var tm = TileManager.Instance;
        if (tm == null || tm.farmableTilemap == null)
        {
            Debug.LogWarning("[Cheat] TileManager/farmableTilemap 없음");
            return;
        }

        Tilemap farmable = tm.farmableTilemap;

        // 플레이어 주변 범위 계산 (tillRadius <= 0 이면 맵 전체)
        bool useRadius = tillRadius > 0;
        Vector3Int center = Vector3Int.zero;
        if (useRadius)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
                center = tm.WorldToCell(player.transform.position);
            else
                useRadius = false; // 플레이어 못 찾으면 전체로 폴백
        }

        int count = 0;
        foreach (var cell in farmable.cellBounds.allPositionsWithin)
        {
            if (!farmable.HasTile(cell)) continue;
            if (tm.IsTilled(cell)) continue;   // 이미 갈린 칸은 건너뜀

            // 범위 제한: 플레이어 셀에서 반경 tillRadius 칸 이내만
            if (useRadius)
            {
                if (Mathf.Abs(cell.x - center.x) > tillRadius) continue;
                if (Mathf.Abs(cell.y - center.y) > tillRadius) continue;
            }

            tm.SetTilled(cell);

            Vector3 centerPos = tm.CellCenter(cell);
            GameObject tileObj = new GameObject("FarmTile");
            tileObj.transform.position = centerPos;

            BoxCollider2D col = tileObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
            col.isTrigger = true;

            FarmTile newTile = tileObj.AddComponent<FarmTile>();
            newTile.Till();

            count++;
        }
        Debug.Log($"[Cheat] 농사 가능 지역 {count}칸 갈았어요! (반경 {(useRadius ? tillRadius.ToString() : "전체")})");
    }

    // F2 : 손에 든 씨앗을, 인벤에 든 개수만큼 빈 갈린 밭에 심기
    void PlantHeldSeedEverywhere()
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return;

        ItemData seed = inv.SelectedItem;
        if (seed == null || seed.itemType != ItemType.Seed || seed.cropData == null)
        {
            Debug.LogWarning("[Cheat] 손에 씨앗을 들고 있어야 해요!");
            return;
        }

        var tm = TileManager.Instance;
        int planted = 0;

        // 씬의 모든 FarmTile 중, 갈렸고 비어있는 곳에 심기
        FarmTile[] tiles = Object.FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
        foreach (var tile in tiles)
        {
            if (inv.GetItemCount(seed) <= 0) break;          // 씨앗 소진
            if (tile.state != FarmTile.TileState.Tilled) continue; // 갈린 빈 밭만
            if (tile.cropData != null) continue;

            if (tile.Plant(seed.cropData))
            {
                inv.RemoveItem(seed, 1);
                if (tm != null)
                {
                    Vector3Int cell = tm.WorldToCell(tile.transform.position);
                    tm.RefreshTile(cell, tile.state);
                }
                planted++;
            }
        }
        Debug.Log($"[Cheat] {seed.itemName} {planted}개 심었어요!");
    }

    // F3 : 모든 작물을 한 단계씩 성장 (누를 때마다 다음 단계)
    void GrowAllCrops()
    {
        var tm = TileManager.Instance;
        FarmTile[] tiles = Object.FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
        int grown = 0;
        foreach (var tile in tiles)
        {
            if (tile.cropData == null) continue;
            tile.GrowOneStage();   // 한 단계씩
            if (tm != null)
            {
                Vector3Int cell = tm.WorldToCell(tile.transform.position);
                tm.RefreshTile(cell, tile.state);
            }
            grown++;
        }
        Debug.Log($"[Cheat] 작물 {grown}개 한 단계 성장!");
    }
}
#endif