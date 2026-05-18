using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;

    public InventorySlot(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [Header("인벤토리 슬롯")]
    public List<InventorySlot> slots = new List<InventorySlot>();
    public int selectedIndex = 0;

    public ItemData SelectedItem =>
        slots.Count > 0 && selectedIndex < slots.Count
        ? slots[selectedIndex].item : null;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
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
            if (Keyboard.current.digit0Key.wasPressedThisFrame) SelectItem(9);
        }
    }

    public void SelectItem(int index)
    {
        if (index < slots.Count)
        {
            selectedIndex = index;
            Debug.Log($"선택된 아이템: {slots[selectedIndex].item.itemName} x{slots[selectedIndex].amount}");
        }
    }

    public void AddItem(ItemData item, int amount = 1)
    {
        foreach (var slot in slots)
        {
            if (slot.item == item)
            {
                slot.amount += amount;
                Debug.Log($"{item.itemName} x{amount} 획득! (보유: {slot.amount})");
                return;
            }
        }
        slots.Add(new InventorySlot(item, amount));
        Debug.Log($"{item.itemName} x{amount} 획득!");
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        foreach (var slot in slots)
        {
            if (slot.item == item)
            {
                if (slot.amount < amount)
                {
                    Debug.Log($"{item.itemName}이 부족해요!");
                    return false;
                }
                slot.amount -= amount;
                if (slot.amount <= 0)
                    slots.Remove(slot);
                Debug.Log($"{item.itemName} x{amount} 소모!");
                return true;
            }
        }
        Debug.Log($"{item.itemName}이 없어요!");
        return false;
    }

    public int GetItemCount(ItemData item)
    {
        foreach (var slot in slots)
            if (slot.item == item)
                return slot.amount;
        return 0;
    }
}