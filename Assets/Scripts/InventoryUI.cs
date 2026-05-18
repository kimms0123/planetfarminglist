using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("슬롯")]
    public Image[] slots;
    public Image[] itemIcons;

    [Header("선택 색상")]
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    public Color selectedColor = new Color(1f, 0.8f, 0f, 0.8f);

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
            slots[i].color = (i == Inventory.Instance.selectedIndex)
                ? selectedColor : normalColor;

            if (i < Inventory.Instance.slots.Count &&
                Inventory.Instance.slots[i] != null &&
                Inventory.Instance.slots[i].item != null &&
                Inventory.Instance.slots[i].item.itemSprite != null)
            {
                itemIcons[i].sprite = Inventory.Instance.slots[i].item.itemSprite;
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