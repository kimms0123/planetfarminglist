using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [Header("인벤토리 슬롯")]
    public List<ItemData> items = new List<ItemData>();
    public int selectedIndex = 0;

    public ItemData SelectedItem =>
        items.Count > 0 ? items[selectedIndex] : null;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // 숫자키로 아이템 선택
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectItem(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectItem(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectItem(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectItem(3);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectItem(4);
            if (Keyboard.current.digit6Key.wasPressedThisFrame) SelectItem(5);
            if (Keyboard.current.digit7Key.wasPressedThisFrame) SelectItem(6);
            if (Keyboard.current.digit8Key.wasPressedThisFrame) SelectItem(7);
            if (Keyboard.current.digit9Key.wasPressedThisFrame) SelectItem(8);
        }
    }

    public void SelectItem(int index)
    {
        if (index < items.Count)
        {
            selectedIndex = index;
            Debug.Log($"선택된 아이템: {items[selectedIndex].itemName}");
        }
    }

    public void AddItem(ItemData item)
    {
        items.Add(item);
    }
}