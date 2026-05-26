using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;

public class ShopSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI 요소")]
    public Image iconImage;
    public TextMeshProUGUI quantityText;
    public GameObject selectedHighlight;
    public GameObject unsellableOverlay;

    private int slotIndex;

    public void Setup(int index)
    {
        slotIndex = index;
    }

    public void Refresh(InventorySlot slot, bool isSelected)
    {
        // 선택 표시
        if (selectedHighlight != null)
            selectedHighlight.SetActive(isSelected);

        if (slot == null || slot.IsEmpty())
        {
            if (iconImage != null) { iconImage.sprite = null; iconImage.color = Color.clear; }
            if (quantityText != null) quantityText.text = "";
            if (unsellableOverlay != null) unsellableOverlay.SetActive(false);
            return;
        }

        // 아이콘
        if (iconImage != null)
        {
            iconImage.sprite = slot.itemData?.itemSprite;
            iconImage.color = slot.itemData?.itemSprite != null ? Color.white : Color.clear;
        }

        // 수량
        if (quantityText != null)
            quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";

        // 판매 불가 오버레이
        bool canSell = slot.itemData != null && slot.itemData.canSell && slot.itemData.sellPrice > 0;
        if (unsellableOverlay != null)
            unsellableOverlay.SetActive(!canSell);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 좌클릭 — 슬롯 선택
        if (eventData.button == PointerEventData.InputButton.Left)
            ShopSellUI.Instance?.SelectSlot(slotIndex);

        // 우클릭 — 빠른 판매
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (Keyboard.current.shiftKey.isPressed)
                ShopSellUI.Instance?.QuickSellAll(slotIndex);
            else
                ShopSellUI.Instance?.QuickSell(slotIndex);
        }
    }
}