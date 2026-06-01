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

    [Header("슬롯 설정")]
    public int slotCount = 10;

    [Header("시작 아이템")]
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

        // 슬롯 초기화
        for (int i = 0; i < slotCount; i++)
            slots.Add(new InventorySlot());

        // 시작 아이템 추가
        foreach (var startingItem in startingItems)
            AddItem(startingItem.item, startingItem.quantity);
    }

    // ─────────────────────────────────────────
    // 슬롯 접근
    // ─────────────────────────────────────────
    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return null;
        return slots[index];
    }

    public List<InventorySlot> GetAllSlots() => slots;

    // ─────────────────────────────────────────
    // 아이템 추가
    // ─────────────────────────────────────────
    public bool AddItem(ItemData item, int amount = 1)
    {
        return AddItemAndReturnLeftover(item, amount) == 0;
    }

    public int AddItemAndReturnLeftover(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return 0;

        int remaining = amount;

        // 1. 같은 아이템 슬롯에 먼저 채우기
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

        // 2. 빈 슬롯에 넣기
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
            Debug.Log($"{item.itemName} x{amount} 획득!");
        else
            Debug.Log($"{item.itemName} x{amount - leftover} 획득! (x{leftover} 인벤 초과)");

        return leftover;
    }

    // ─────────────────────────────────────────
    // 아이템 제거
    // ─────────────────────────────────────────
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
            Debug.Log($"{item.itemName} 수량 부족!");
            return false;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    // ─────────────────────────────────────────
    // 슬롯 교체 / 이동
    // ─────────────────────────────────────────
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

    // ─────────────────────────────────────────
    // 유틸
    // ─────────────────────────────────────────
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
    // ─────────────────────────────────────────
    // 판매 관련
    // ─────────────────────────────────────────
    public bool CanSellSlot(int index)
    {
        var slot = GetSlot(index);
        if (slot == null || slot.IsEmpty()) return false;
        if (!slot.itemData.canSell) return false;
        if (slot.itemData.sellPrice <= 0) return false;
        return true;
    }

    public int SellOneFromSlot(int index)
    {
        if (!CanSellSlot(index)) return 0;
        var slot = GetSlot(index);
        int price = slot.itemData.sellPrice;
        slot.quantity--;
        if (slot.quantity <= 0) slot.Clear();
        OnInventoryChanged?.Invoke();
        return price;
    }

    public int SellAllFromSlot(int index)
    {
        if (!CanSellSlot(index)) return 0;
        var slot = GetSlot(index);
        int totalPrice = slot.itemData.sellPrice * slot.quantity;
        slot.Clear();
        OnInventoryChanged?.Invoke();
        return totalPrice;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slotCount) return;
        SelectedIndex = index;
        OnInventoryChanged?.Invoke();
    }
}