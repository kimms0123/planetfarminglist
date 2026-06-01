using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InventoryWindowUI : MonoBehaviour
{
    public static InventoryWindowUI Instance;

    [Header("패널")]
    public GameObject inventoryWindowPanel;

    [Header("슬롯 UI")]
    public InventorySlotUI[] slotUIs;

    [Header("아이템 설명")]
    public TextMeshProUGUI itemDescriptionText;

    private bool isOpen = false;
    public bool IsOpen => isOpen;
    private int pendingSlotIndex = -1;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RefreshUI;
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
    }

    void Start()
    {
        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].Setup(i, true);
        inventoryWindowPanel.SetActive(false);
        RefreshUI();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // ESC 닫기
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (pendingSlotIndex >= 0)
                ClearPending();
            else if (isOpen)
                CloseInventory();
        }
    }

    public void ToggleInventory()
    {
        Debug.Log($"ToggleInventory 호출! isOpen:{isOpen}");
        if (isOpen) CloseInventory();
        else OpenInventory();
    }

    void OpenInventory()
    {
        isOpen = true;
        inventoryWindowPanel.SetActive(true);
        RefreshUI();
    }

    void CloseInventory()
    {
        isOpen = false;
        inventoryWindowPanel.SetActive(false);
        ClearPending();
    }

    public void OnSlotRightClick(int index)
    {
        if (pendingSlotIndex < 0)
        {
            pendingSlotIndex = index;
            Debug.Log($"슬롯 {index} 선택됨");
        }
        else
        {
            if (pendingSlotIndex != index)
                InventoryManager.Instance?.SwapSlots(pendingSlotIndex, index);
            ClearPending();
        }
        RefreshUI();
    }

    void ClearPending()
    {
        pendingSlotIndex = -1;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (InventoryManager.Instance == null) return;
        for (int i = 0; i < slotUIs.Length; i++)
        {
            var slot = InventoryManager.Instance.GetSlot(i);
            bool isSelected = (i == InventoryManager.Instance.SelectedIndex);
            bool isPending = (i == pendingSlotIndex);
            slotUIs[i].Refresh(slot, isSelected, isPending);

            if (isPending && slot != null && !slot.IsEmpty() && itemDescriptionText != null)
                itemDescriptionText.text = slot.itemData.itemName;
        }
        if (pendingSlotIndex < 0 && itemDescriptionText != null)
            itemDescriptionText.text = "";
    }
}