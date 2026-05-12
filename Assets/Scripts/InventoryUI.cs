using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("슬롯")]
    public Image[] slots;           // 슬롯 배경
    public Image[] itemIcons;       // 아이템 아이콘

    [Header("선택 색상")]
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    public Color selectedColor = new Color(1f, 0.8f, 0f, 0.8f);  // 노란색

    void Start()
    {
        RefreshUI();
    }

    void Update()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (Inventory.Instance == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            // 선택된 슬롯 강조
            slots[i].color = (i == Inventory.Instance.selectedIndex)
                ? selectedColor
                : normalColor;

            // 아이템 아이콘 표시
            if (i < Inventory.Instance.items.Count &&
                Inventory.Instance.items[i] != null &&
                Inventory.Instance.items[i].itemSprite != null)
            {
                itemIcons[i].sprite = Inventory.Instance.items[i].itemSprite;
                itemIcons[i].color = Color.white;
            }
            else
            {
                itemIcons[i].sprite = null;
                itemIcons[i].color = Color.clear;
            }
        }
    }
}