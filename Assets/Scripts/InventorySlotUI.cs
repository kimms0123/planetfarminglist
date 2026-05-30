using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [Header("UI 요소")]
    public Image itemIcon;
    public TextMeshProUGUI quantityText;

    [Header("색상")]
    public Color normalColor = new Color(1f, 1f, 1f, 1f);
    public Color selectedColor = new Color(1f, 1f, 0.7f, 1f);
    public Color pendingMoveColor = new Color(0.7f, 0.9f, 1f, 1f);

    private Image background;
    private int slotIndex;

    // 드래그용 static 변수
    private static GameObject dragIcon;
    private static InventorySlotUI dragSource;
    private Canvas canvas;

    void Awake()
    {
        background = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();

        if (itemIcon == null)
        {
            Transform iconTransform = transform.Find("ItemIcon");
            if (iconTransform != null)
                itemIcon = iconTransform.GetComponent<Image>();
        }

        if (quantityText == null)
        {
            Transform textTransform = transform.Find("QuantityText");
            if (textTransform != null)
                quantityText = textTransform.GetComponent<TextMeshProUGUI>();
        }
    }

    public void Setup(int index, bool isWindow)
    {
        slotIndex = index;
    }

    public void Refresh(InventorySlot slot, bool isSelected, bool isPending)
    {
        if (background != null)
        {
            if (isPending) background.color = pendingMoveColor;
            else if (isSelected) background.color = selectedColor;
            else background.color = normalColor;
        }

        if (itemIcon == null) return;

        if (slot != null && !slot.IsEmpty() && slot.itemData?.itemSprite != null)
        {
            itemIcon.sprite = slot.itemData.itemSprite;
            itemIcon.color = Color.white;
            if (quantityText != null)
                quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.clear;
            if (quantityText != null)
                quantityText.text = "";
        }
    }

    // ─────────────────────────────────────────
    // 드래그 시작
    // ─────────────────────────────────────────
    public void OnBeginDrag(PointerEventData eventData)
    {
        var slot = InventoryManager.Instance?.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty()) return;

        dragSource = this;

        // 드래그 아이콘 생성
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        Image icon = dragIcon.AddComponent<Image>();
        icon.sprite = slot.itemData?.itemSprite;
        icon.raycastTarget = false; // ★ 레이캐스트 막으면 안 됨

        RectTransform rt = dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60, 60);

        // 원래 아이콘 반투명
        if (itemIcon != null)
            itemIcon.color = new Color(1, 1, 1, 0.3f);

        Debug.Log($"드래그 시작! 슬롯:{slotIndex}");
    }

    // ─────────────────────────────────────────
    // 드래그 중 — 아이콘 마우스 따라다니기
    // ─────────────────────────────────────────
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint);

        dragIcon.GetComponent<RectTransform>().localPosition = localPoint;
    }

    // ─────────────────────────────────────────
    // 드래그 끝 — 빈 곳에 드롭하면 취소
    // ─────────────────────────────────────────
    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }

        // 원래 아이콘 복구
        if (itemIcon != null)
        {
            var slot = InventoryManager.Instance?.GetSlot(slotIndex);
            itemIcon.color = (slot != null && !slot.IsEmpty()) ? Color.white : Color.clear;
        }

        dragSource = null;
        Debug.Log("드래그 종료");
    }

    // ─────────────────────────────────────────
    // 드롭 받기 — 슬롯 교체
    // ─────────────────────────────────────────
    public void OnDrop(PointerEventData eventData)
    {
        if (dragSource == null) return;
        if (dragSource == this) return;

        InventoryManager.Instance?.SwapSlots(dragSource.slotIndex, slotIndex);
        Debug.Log($"드롭! {dragSource.slotIndex} → {slotIndex}");
    }

    // ─────────────────────────────────────────
    // 우클릭 — 기존 유지
    // ─────────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            InventoryWindowUI.Instance?.OnSlotRightClick(slotIndex);
    }
}