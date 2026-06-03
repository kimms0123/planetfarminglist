using UnityEngine;

// 플레이어가 도구 외 아이템을 들고 있을 때, 머리 위(손 사이)에 그 아이템 스프라이트를 표시.
//
// 세팅:
//  1) Player 자식으로 빈 오브젝트 + SpriteRenderer 만들기 → 이름 HeldItem
//  2) 위치를 플레이어 머리 위(만세 손 사이)로 조정
//  3) 이 스크립트를 HeldItem(또는 Player)에 붙이고 heldRenderer에 그 SpriteRenderer 연결
public class HeldItemDisplay : MonoBehaviour
{
    [Header("아이템을 그릴 SpriteRenderer (머리 위)")]
    public SpriteRenderer heldRenderer;

    [Header("플레이어보다 위에 보이도록 sorting order")]
    public int sortingOrder = 20;

    void Start()
    {
        if (heldRenderer == null)
            heldRenderer = GetComponent<SpriteRenderer>();

        if (heldRenderer != null)
            heldRenderer.sortingOrder = sortingOrder;
    }

    void Update()
    {
        if (heldRenderer == null) return;

        Sprite icon = GetHeldIcon();

        if (icon != null)
        {
            heldRenderer.enabled = true;
            heldRenderer.sprite = icon;
        }
        else
        {
            heldRenderer.enabled = false; // 안 들고 있으면 숨김
        }
    }

    // 도구가 아닌 선택 아이템이면 그 아이콘 반환, 아니면 null
    Sprite GetHeldIcon()
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return null;

        ItemData selected = inv.SelectedItem;
        if (selected == null) return null;
        if (selected.itemType == ItemType.Tool) return null; // 도구는 안 듦

        // ItemData에 아이콘 필드가 있으면 그걸 사용.
        // 보통 icon 또는 itemIcon / sprite 중 하나임. 프로젝트에 맞게 한 줄만 남기세요.
        return selected.itemSprite;
    }
}