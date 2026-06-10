using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// 구매 슬롯 (스듀식 입력)
//  - 좌클릭         → 1개 구매
//  - Shift + 좌클릭 → 10개 구매
public class ShopBuySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;

    [Header("살 수 없을 때 흐리게 (선택)")]
    public bool dimWhenCantAfford = true;

    private ItemData item;
    private int price;
    private ShopBuyUI shop;

    public void Setup(ItemData item, int price, ShopBuyUI shop)
    {
        this.item = item;
        this.price = price;
        this.shop = shop;

        if (iconImage != null)
        {
            iconImage.sprite = item.itemSprite;
            iconImage.enabled = (item.itemSprite != null);
            iconImage.preserveAspect = true;
        }
        if (nameText != null) nameText.text = item.itemName;
        if (priceText != null) priceText.text = $"{price} G";

        RefreshAffordable();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 좌클릭만 구매 처리
        if (eventData.button != PointerEventData.InputButton.Left) return;

        bool shift = Keyboard.current != null &&
                     (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        int amount = shift ? 10 : 1;   // Shift+좌클릭 = 10개
        if (shop != null) shop.TryBuy(item, price, amount);
    }

    // 돈 충분 여부에 따라 살짝 흐리게 (시각 피드백)
    public void RefreshAffordable()
    {
        if (!dimWhenCantAfford || iconImage == null) return;
        if (MoneyManager.Instance == null) return;

        bool canAfford = MoneyManager.Instance.CurrentMoney >= price;
        var c = iconImage.color;
        c.a = canAfford ? 1f : 0.4f;
        iconImage.color = c;
    }
}