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
    private int pendingSlotIndex = -1; // 우클릭 이동 대기 슬롯

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
        // ★ Input System 방식으로 변경
        if (Keyboard.current.tabKey.wasPressedThisFrame)
            ToggleInventory();

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
        if (isOpen) CloseInventory();
        else OpenInventory();
    }

    void OpenInventory()
    {
        isOpen = true;
        inventoryWindowPanel.SetActive(true);
        // ★ 인벤토리 열어도 이동은 가능하게 (잠금 제거)
        // PlayerController.IsInputLocked = true;
        RefreshUI();
    }

    void CloseInventory()
    {
        isOpen = false;
        inventoryWindowPanel.SetActive(false);
        // PlayerController.IsInputLocked = false;
        ClearPending();
    }

    // ─────────────────────────────────────────
    // 우클릭 슬롯 이동
    // ─────────────────────────────────────────
    public void OnSlotRightClick(int index)
    {
        if (pendingSlotIndex < 0)
        {
            // 첫 번째 클릭 — 선택
            pendingSlotIndex = index;
            Debug.Log($"슬롯 {index} 선택됨");
        }
        else
        {
            // 두 번째 클릭 — 이동/교체
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

            // 설명 텍스트
            if (isPending && slot != null && !slot.IsEmpty() && itemDescriptionText != null)
                itemDescriptionText.text = slot.itemData.itemName;
        }

        if (pendingSlotIndex < 0 && itemDescriptionText != null)
            itemDescriptionText.text = "";
    }

}