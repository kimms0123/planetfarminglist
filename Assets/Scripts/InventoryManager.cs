using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int quantity;

    public bool IsEmpty() => itemData == null || quantity <= 0;

    public void Clear()
    {
        itemData = null;
        quantity = 0;
    }
}

[System.Serializable]
public class StartingItem
{
    public ItemData item;
    public int quantity = 1;
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("½½·Ô ¼³Á¤")]
    public int slotCount = 10;

    [Header("½ÃÀÛ ¾ÆÀÌÅÛ")]
    public List<StartingItem> startingItems = new List<StartingItem>();

    private List<InventorySlot> slots = new List<InventorySlot>();

    public event Action OnInventoryChanged;

    public int SelectedIndex { get; private set; } = 0;
    public ItemData SelectedItem =>
        slots.Count > SelectedIndex && !slots[SelectedIndex].IsEmpty()
        ? slots[SelectedIndex].itemData : null;

    void Awake()
    {
        Instance = this;

        // ½½·Ô ÃÊ±âÈ­
        for (int i = 0; i < slotCount; i++)
            slots.Add(new InventorySlot());

        // ½ÃÀÛ ¾ÆÀÌÅÛ Ãß°¡
        foreach (var startingItem in startingItems)
            AddItem(startingItem.item, startingItem.quantity);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ½½·Ô Á¢±Ù
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return null;
        return slots[index];
    }

    public List<InventorySlot> GetAllSlots() => slots;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¾ÆÀÌÅÛ Ãß°¡
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public bool AddItem(ItemData item, int amount = 1)
    {
        return AddItemAndReturnLeftover(item, amount) == 0;
    }

    public int AddItemAndReturnLeftover(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return 0;

        int remaining = amount;

        // 1. °°Àº ¾ÆÀÌÅÛ ½½·Ô¿¡ ¸ÕÀú Ã¤¿ì±â
        if (item.canStack)
        {
            foreach (var slot in slots)
            {
                if (slot.itemData == item && slot.quantity < item.maxStack)
                {
                    int canAdd = item.maxStack - slot.quantity;
                    int toAdd = Mathf.Min(canAdd, remaining);
                    slot.quantity += toAdd;
                    remaining -= toAdd;
                    if (remaining <= 0) break;
                }
            }
        }

        // 2. ºó ½½·Ô¿¡ ³Ö±â
        if (remaining > 0)
        {
            foreach (var slot in slots)
            {
                if (slot.IsEmpty())
                {
                    int toAdd = item.canStack
                        ? Mathf.Min(item.maxStack, remaining)
                        : 1;
                    slot.itemData = item;
                    slot.quantity = toAdd;
                    remaining -= toAdd;
                    if (remaining <= 0) break;
                }
            }
        }

        OnInventoryChanged?.Invoke();
        int leftover = remaining;
        if (leftover == 0)
            Debug.Log($"{item.itemName} x{amount} È¹µæ!");
        else
            Debug.Log($"{item.itemName} x{amount - leftover} È¹µæ! (x{leftover} ÀÎº¥ ÃÊ°ú)");

        return leftover;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¾ÆÀÌÅÛ Á¦°Å
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null) return false;

        int remaining = amount;
        foreach (var slot in slots)
        {
            if (slot.itemData == item)
            {
                int toRemove = Mathf.Min(slot.quantity, remaining);
                slot.quantity -= toRemove;
                remaining -= toRemove;
                if (slot.quantity <= 0) slot.Clear();
                if (remaining <= 0) break;
            }
        }

        if (remaining > 0)
        {
            Debug.Log($"{item.itemName} ¼ö·® ºÎÁ·!");
            return false;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ½½·Ô ±³Ã¼ / ÀÌµ¿
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Count) return;
        if (indexB < 0 || indexB >= slots.Count) return;

        var slotA = slots[indexA];
        var slotB = slots[indexB];

        if (!slotA.IsEmpty() && !slotB.IsEmpty()
            && slotA.itemData == slotB.itemData
            && slotA.itemData.canStack)
        {
            int canAdd = slotA.itemData.maxStack - slotB.quantity;
            int toMove = Mathf.Min(canAdd, slotA.quantity);
            slotB.quantity += toMove;
            slotA.quantity -= toMove;
            if (slotA.quantity <= 0) slotA.Clear();
        }
        else
        {
            var tempData = slotA.itemData;
            var tempQty = slotA.quantity;
            slotA.itemData = slotB.itemData;
            slotA.quantity = slotB.quantity;
            slotB.itemData = tempData;
            slotB.quantity = tempQty;
            if (slotA.IsEmpty()) slotA.Clear();
            if (slotB.IsEmpty()) slotB.Clear();
        }

        OnInventoryChanged?.Invoke();
    }

    public void ClearSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        slots[index].Clear();
        OnInventoryChanged?.Invoke();
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // À¯Æ¿
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public bool IsFull()
    {
        foreach (var slot in slots)
            if (slot.IsEmpty()) return false;
        return true;
    }

    public int GetEmptySlotCount()
    {
        int count = 0;
        foreach (var slot in slots)
            if (slot.IsEmpty()) count++;
        return count;
    }

    public int GetItemCount(ItemData item)
    {
        int count = 0;
        foreach (var slot in slots)
            if (slot.itemData == item) count += slot.quantity;
        return count;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slotCount) return;
        SelectedIndex = index;
        OnInventoryChanged?.Invoke();
    }
}