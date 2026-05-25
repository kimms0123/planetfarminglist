using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI 요소")]
    public Image itemIcon;
    public TextMeshProUGUI quantityText;

    [Header("색상")]
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    public Color selectedColor = new Color(1f, 0.8f, 0f, 0.8f);
    public Color pendingMoveColor = new Color(0f, 0.8f, 1f, 0.8f);

    private Image background;
    private int slotIndex;

    void Awake()
    {
        background = GetComponent<Image>();

        // ★ 자동 연결 — Inspector에서 연결 안 해도 자동으로 찾기
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
        // 배경 색상
        if (background != null)
        {
            if (isPending) background.color = pendingMoveColor;
            else if (isSelected) background.color = selectedColor;
            else background.color = normalColor;
        }

        // 아이콘 — null 체크 추가
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

    public void OnRightClick()
    {
        InventoryWindowUI.Instance?.OnSlotRightClick(slotIndex);
    }
}