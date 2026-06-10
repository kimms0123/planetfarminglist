using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarUI : MonoBehaviour
{
    public static HotbarUI Instance;

    [Header("슬롯 UI")]
    public InventorySlotUI[] slotUIs;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        // 비워두기
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
    }

    void Start()
    {
        // ★ Start에서 이벤트 구독
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RefreshUI;

        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].Setup(i, false);

        RefreshUI();
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame) Select(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) Select(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) Select(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) Select(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) Select(4);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) Select(5);
        if (Keyboard.current.digit7Key.wasPressedThisFrame) Select(6);
        if (Keyboard.current.digit8Key.wasPressedThisFrame) Select(7);
        if (Keyboard.current.digit9Key.wasPressedThisFrame) Select(8);
        if (Keyboard.current.digit0Key.wasPressedThisFrame) Select(9);
    }

    void Select(int index)
    {
        InventoryManager.Instance?.SelectSlot(index);
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (InventoryManager.Instance == null) return;
        for (int i = 0; i < slotUIs.Length; i++)
        {
            var slot = InventoryManager.Instance.GetSlot(i);
            bool isSelected = (i == InventoryManager.Instance.SelectedIndex);
            slotUIs[i].Refresh(slot, isSelected, false);
        }
    }
}