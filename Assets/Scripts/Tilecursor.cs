using UnityEngine;
using UnityEngine.InputSystem;

// 도구를 들었을 때, 마우스가 가리키는 타일 위에 네모 커서를 표시.
//  - 마우스 화살표는 숨김 (게임 플레이 중)
//  - 인벤토리/상점 등 UI가 열리면 화살표 다시 표시
//
// 세팅:
//  1) 빈 오브젝트에 SpriteRenderer + 이 스크립트 (네모 커서 스프라이트 연결)
//  2) cursorRenderer에 그 SpriteRenderer 연결 (비우면 자동)
//  3) 농장 씬에 배치 (커서는 농장에서만 의미 있음)
public class TileCursor : MonoBehaviour
{
    [Header("커서 스프라이트")]
    public SpriteRenderer cursorRenderer;

    [Header("정렬 순서 (타일/작물 위에 보이게)")]
    public int sortingOrder = 10;

    [Header("마우스 화살표 숨기기")]
    public bool hideSystemCursor = true;

    private Camera cam;

    void Start()
    {
        if (cursorRenderer == null)
            cursorRenderer = GetComponent<SpriteRenderer>();
        if (cursorRenderer != null)
            cursorRenderer.sortingOrder = sortingOrder;

        cam = Camera.main;
    }

    Camera GetCamera()
    {
        if (cam == null) cam = Camera.main;
        return cam;
    }

    void Update()
    {
        if (cursorRenderer == null) return;

        // UI(인벤토리/상점)가 열려있으면 → 시스템 커서 보이고, 타일 커서 숨김
        bool uiOpen =
            (InventoryWindowUI.Instance != null && InventoryWindowUI.Instance.IsOpen) ||
            (ShopSellUI.Instance != null && ShopSellUI.Instance.IsShopOpen) ||
            PlayerController.IsInputLocked;

        if (uiOpen)
        {
            Cursor.visible = true;          // UI 조작 위해 화살표 보임
            cursorRenderer.enabled = false; // 타일 커서 숨김
            return;
        }

        // 도구 또는 씨앗을 들고 있는지 확인 (이때만 타일 커서 표시)
        bool showCursor = false;
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            ItemData selected = inv.SelectedItem;
            if (selected != null &&
                (selected.itemType == ItemType.Tool || selected.itemType == ItemType.Seed))
                showCursor = true;
        }

        if (!showCursor)
        {
            // 도구 안 들었으면 타일 커서 숨김. 화살표는 숨긴 채 둘지 선택.
            cursorRenderer.enabled = false;
            Cursor.visible = !hideSystemCursor ? true : false;
            return;
        }

        // 도구/씨앗 들었음 → 마우스 화살표 숨기고, 마우스가 가리키는 타일에 네모 커서
        Cursor.visible = !hideSystemCursor;  // hideSystemCursor=true면 화살표 숨김

        Camera c = GetCamera();
        if (c == null) return;
        if (TileManager.Instance == null) { cursorRenderer.enabled = false; return; }

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 world = c.ScreenToWorldPoint(mouseScreen);
        world.z = 0;

        // 타일 칸 중앙으로 스냅
        Vector3Int cell = TileManager.Instance.WorldToCell(world);
        Vector3 center = TileManager.Instance.CellCenter(cell);

        cursorRenderer.enabled = true;
        transform.position = center;
    }

    void OnDisable()
    {
        // 비활성/씬 떠날 때 화살표 복구
        Cursor.visible = true;
    }
}